using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;
using Microsoft.Deployment.Common.Helpers;

using Trinet.Core.IO.Ntfs;
using System.Diagnostics;

namespace Microsoft.Deployment.Actions.Custom.SCCM
{
    [Export(typeof(IAction))]
    public class InstallSCCM : BaseAction
    {
        public const string RESOURCE_PATH = @"Service\Resources\Scripts\";

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Kill any running azurebcp
            NTHelper.KillProcess("azurebcp");

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            Parallel.ForEach(files, (currentFile) =>
            {
                string temppath = Path.Combine(destDirName, currentFile.Name);
                currentFile.CopyTo(temppath, true);
                FileInfo destination = new FileInfo(temppath);
                try
                {
                    if (destination.AlternateDataStreamExists("Zone.Identifier"))
                        destination.DeleteAlternateDataStream("Zone.Identifier");
                }
                catch { }
               
            });

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            string targetPath = request.DataStore.GetValue("TargetPath") ?? FileUtility.GetLocalTemplatePath(request.Info.AppName);
            
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            DirectoryCopy(Path.Combine(request.Info.App.AppFilePath, RESOURCE_PATH), targetPath, true);
            
            return new ActionResponse(ActionStatus.Success, JsonUtility.GetEmptyJObject());
        }
    }
}