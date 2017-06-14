import { DataStoreType } from '../enums/data-store-type';

import { ActionResponse } from '../models/action-response';
import { DataMovementType } from '../models/data-movement-type';
import { InformaticaAgent } from '../models/informatica-agent';
import { ScribeAgent } from '../models/scribe-agent';
import { ScribeAgentInstall } from '../models/scribe-agent-install';
import { ScribeOrganization } from '../models/scribe-organization';

import { ViewModelBase } from '../services/view-model-base';

export class DataMovement extends ViewModelBase {
    dataMovement: string = '';
    dataMovementType: DataMovementType = new DataMovementType();
    informaticaAgentId: string = '';
    informaticaAgentLocation: string = '';
    informaticaAgents: InformaticaAgent[] = [];
    password: string = '';
    scribeAgentId: string = '';
    scribeAgentInstall: ScribeAgentInstall = new ScribeAgentInstall();
    scribeAgents: ScribeAgent[] = [];
    scribeOrganizationId: string = '';
    scribeOrganizations: ScribeOrganization[] = [];
    showAdf: boolean = true;
    showD365: boolean = false;
    showInformatica: boolean = true;
    showScribe: boolean = true;
    subtitle: string = this.MS.Translate.DATA_MOVEMENT_SUBTITLE;
    username: string = '';

    OnDataMovementChanged(): void {
        this.Invalidate();
        this.isValidated = this.dataMovement === this.dataMovementType.ADF || this.dataMovement === this.dataMovementType.D365;
    }

    async OnLoaded(): Promise<void> {
        this.isValidated = this.dataMovement === this.dataMovementType.ADF || this.dataMovement === this.dataMovementType.D365;
    }

    async OnScribeOrganizationChanged(): Promise<void> {
        if (this.MS.HttpService.isOnPremise) {
            this.MS.DataStore.addToDataStore('ScribeOrganizationId', this.scribeOrganizationId, DataStoreType.Private);

            let responseScribeAgents: ActionResponse = await this.MS.HttpService.executeAsync('Microsoft-GetScribeAgents');

            if (responseScribeAgents.IsSuccess) {
                let scribeAgents: ScribeAgent[] = JSON.parse(responseScribeAgents.Body.value);

                for (let i = 0; i < scribeAgents.length; i++) {
                    let scribeAgent: ScribeAgent = scribeAgents[i];
                    if (!scribeAgent.isCloudAgent) {
                        this.scribeAgents.push(scribeAgent);
                    }
                }

                if (this.scribeAgents && this.scribeAgents.length > 0) {
                    this.scribeAgentId = this.scribeAgents[0].id;
                    this.isValidated = true;
                    this.showValidation = true;
                } else {
                    let responseScribeAgentInstall: ActionResponse = await this.MS.HttpService.executeAsync('Microsoft-GetScribeAgentInstall');
                    if (responseScribeAgentInstall.IsSuccess) {
                        this.scribeAgentInstall = JSON.parse(responseScribeAgentInstall.Body.value);
                    }
                }
            }
        }
    }

    async OnValidate(): Promise<boolean> {
        this.Invalidate();

        switch (this.dataMovement) {
            case this.dataMovementType.Informatica:
                this.MS.DataStore.addToDataStore('InformaticaUsername', this.username, DataStoreType.Private);
                this.MS.DataStore.addToDataStore('InformaticaPassword', this.password, DataStoreType.Private);

                let responseInformatica: ActionResponse = await this.MS.HttpService.executeAsync('Microsoft-VerifyInformaticaCredentials');

                if (responseInformatica.IsSuccess) {
                    if (this.MS.HttpService.isOnPremise) {
                        let responseInformaticaAgents: ActionResponse = await this.MS.HttpService.executeAsync('Microsoft-GetInformaticaAgents');
                        if (responseInformaticaAgents.IsSuccess) {
                            this.informaticaAgents = JSON.parse(responseInformaticaAgents.Body.value);

                            if (this.informaticaAgents && this.informaticaAgents.length > 0) {
                                this.informaticaAgentId = this.informaticaAgents[0].id;
                                this.isValidated = true;
                                this.showValidation = true;
                            } else {
                                let responseInformaticaAgentLocation: ActionResponse = await this.MS.HttpService.executeAsync('Microsoft-GetInformaticaAgentLocation');
                                if (responseInformaticaAgentLocation.IsSuccess) {
                                    this.informaticaAgentLocation = JSON.parse(responseInformaticaAgentLocation.Body.value);
                                }
                            }
                        }
                    } else {
                        this.MS.DataStore.addToDataStore('InformaticaAgentName', 'Informatica Cloud Hosted Agent', DataStoreType.Public);
                        this.isValidated = true;
                        this.showValidation = true;
                    }
                }

                break;
            case this.dataMovementType.Scribe:
                this.MS.DataStore.addToDataStore('ScribeUsername', this.username, DataStoreType.Private);
                this.MS.DataStore.addToDataStore('ScribePassword', this.password, DataStoreType.Private);

                let responseScribe: ActionResponse = await this.MS.HttpService.executeAsync('Microsoft-GetScribeOrganizations');

                if (responseScribe.IsSuccess) {
                    this.scribeOrganizations = JSON.parse(responseScribe.Body.value);

                    if (this.scribeOrganizations && this.scribeOrganizations.length > 0) {
                        this.scribeOrganizationId = this.scribeOrganizations[0].id;

                        if (this.MS.HttpService.isOnPremise) {
                            this.OnScribeOrganizationChanged();
                        } else {
                            this.MS.DataStore.addToDataStore('ScribeAgentName', 'Cloud Agent', DataStoreType.Public);

                            this.isValidated = true;
                            this.showValidation = true;
                        }
                    }
                }

                break;
        }

        return this.isValidated;
    }

    async NavigatingNext(): Promise<boolean> {
        this.MS.DataStore.addToDataStore('DataMovement', this.dataMovement, DataStoreType.Public);

        switch (this.dataMovement) {
            case this.dataMovementType.Informatica:
                if (this.MS.HttpService.isOnPremise) {
                    let informaticaAgent: InformaticaAgent = this.informaticaAgents.find(x => x.id === this.informaticaAgentId);
                    this.MS.DataStore.addToDataStore('InformaticaAgentName', informaticaAgent.name, DataStoreType.Public);
                }
                break;
            case this.dataMovementType.Scribe:
                if (this.MS.HttpService.isOnPremise) {
                    let scribeAgent: ScribeAgent = this.scribeAgents.find(x => x.id === this.scribeAgentId);
                    this.MS.DataStore.addToDataStore('ScribeAgentName', scribeAgent.name, DataStoreType.Public);
                }
                let scribeOrganization: ScribeOrganization = this.scribeOrganizations.find(x => x.id === this.scribeOrganizationId);
                this.MS.DataStore.addToDataStore('ScribeApiToken', scribeOrganization.apiToken, DataStoreType.Private);
                this.MS.DataStore.addToDataStore('ScribeOrganizationId', scribeOrganization.id, DataStoreType.Private);
                this.MS.DataStore.addToDataStore('azureSqlDisabled', 'true', DataStoreType.Public);
                this.MS.DataStore.addToDataStoreWithCustomRoute('ssas', 'ssasDisabled', 'true', DataStoreType.Public);
                break;
        }

        return true;
    }
}