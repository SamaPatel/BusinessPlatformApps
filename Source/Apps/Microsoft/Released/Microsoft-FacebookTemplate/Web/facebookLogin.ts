import { DataStoreType } from '../../../../../SiteCommon/Web/enums/data-store-type';

import { ViewModelBase } from '../../../../../SiteCommon/Web/services/view-model-base';

export class FacebookLogin extends ViewModelBase {
    facebookClientId: string = '';
    facebookClientSecret: string = '';

    constructor() {
        super();
    }

    async OnLoaded(): Promise<void> {
        this.isValidated = false;
    }

    async OnValidate(): Promise<boolean> {
        if (this.facebookClientId.length > 0 && this.facebookClientSecret.length > 0) {
            let body: any = {};
            body.FacebookClientId = this.facebookClientId;
            body.FacebookClientSecret = this.facebookClientSecret;
            let result = await this.MS.HttpService.executeAsync('Microsoft-ValidateFacebookDeveloperAccount', body);
            if (!result.IsSuccess) {
                return false;
            }

            this.isValidated = true;
            this.showValidation = true;

            this.MS.DataStore.addToDataStore("FacebookClientId", this.facebookClientId, DataStoreType.Private);
            this.MS.DataStore.addToDataStore("FacebookClientSecret", this.facebookClientSecret, DataStoreType.Private);
            return true;
        }

        return false;
    }
}