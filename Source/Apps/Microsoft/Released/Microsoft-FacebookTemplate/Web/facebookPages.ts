import { DataStoreType } from '../../../../../SiteCommon/Web/enums/data-store-type';

import { ViewModelBase } from '../../../../../SiteCommon/Web/services/view-model-base';

export class facebookPages extends ViewModelBase {
    searchQuery: string = '';

    constructor() {
        super();
    }

    async OnLoaded(): Promise<void> {
        this.isValidated = false;
    }

    async OnValidate(): Promise<boolean> {
        if (this.searchQuery.length > 0) {
            let body: any = {};
            body.FacebookPages = this.searchQuery;
            let result = await this.MS.HttpService.executeAsync('Microsoft-ValidateFacebookPage', body);
            if (!result.IsSuccess) {
                return false;
            }
            this.MS.DataStore.addToDataStore("FacebookPagesToFollow", this.searchQuery, DataStoreType.Public);
            this.isValidated = true;
            this.showValidation = true;
            return true;
        }

        return false;
    }
}