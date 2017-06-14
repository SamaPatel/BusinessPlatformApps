﻿import { SqlServerValidationUtility } from '../classes/sql-server-validation-utility';

import { DataStoreType } from '../enums/data-store-type';

import { ActionResponse } from '../models/action-response';
import { AzureLocation } from '../models/azure-location';

import { ViewModelBase } from '../services/view-model-base';

export class SqlServer extends ViewModelBase {
    subtitle: string = '';
    title: string = '';

    auth: string = 'Windows';
    azureLocations: AzureLocation[] = [];
    azureSqlSuffix: string = '.database.windows.net';
    azureGovtSuffix: string = '.database.usgovcloudapi.net';
    checkSqlVersion: boolean = false;
    database: string = null;
    databases: string[] = [];
    hideSqlAuth: boolean = false;
    isAzureSql: boolean = false;
    isGovAzureSql: boolean = false;
    isWindowsAuth: boolean = true;

    newSqlDatabase: string = null;
    password: string = '';
    passwordConfirmation: string = '';
    showAllWriteableDatabases: boolean = true;
    showAzureSql: boolean = true;
    showGovAzure: boolean = false;

    showCredsWhenWindowsAuth: boolean = false;
    showDatabases: boolean = false;
    showNewSqlOption: boolean = false;
    showSkuS1: boolean = true;
    showSqlRecoveryModeHint: boolean = false;
    sqlInstance: string = 'ExistingSql';
    sqlLocation: string = '';
    sqlServer: string = '';
    sqlSku: string = 'S1';
    username: string = '';
    validateWindowsCredentials: boolean = false;
    validationTextBox: string = '';

    credentialTarget: string = '';
    useImpersonation: boolean = false;

    constructor() {
        super();
    }

    async OnLoaded(): Promise<void> {
        this.Invalidate();

        if (this.showNewSqlOption) {
            if (!this.azureLocations || this.azureLocations.length === 0) {
                let locationsResponse: ActionResponse = await this.MS.HttpService.executeAsync('Microsoft-GetLocations', {});
                if (locationsResponse.IsSuccess) {
                    this.azureLocations = locationsResponse.Body.value;
                    if (this.azureLocations && this.azureLocations.length > 5) {
                        this.sqlLocation = this.azureLocations[5].Name;
                    }
                }
            }
        }
    }

    Invalidate(): void {
        super.Invalidate();
        this.database = null;
        this.databases = [];
        this.showDatabases = false;
    }

    onAuthChange(): void {
        this.database = null;
        this.databases = [];
        this.showDatabases = false;
        if(this.auth === 'SQL Server'){
            this.isWindowsAuth = false;
        } else {
            this.isWindowsAuth=true;
        }
        this.username = '';
        this.password = '';
    } 

    async OnValidate(): Promise<boolean> {
        this.Invalidate();

        this.sqlServer = this.sqlServer.toLowerCase();
        if (this.sqlInstance === 'ExistingSql') {
            let databasesResponse = await this.GetDatabases();
            if (databasesResponse.IsSuccess) {
                this.databases = databasesResponse.Body.value;
                this.database = this.databases[0];
                this.isValidated = true;
                this.showDatabases = true;
            } else {
                this.isValidated = false;
                this.showDatabases = false;
            }
        } else if (this.sqlInstance === 'NewSql') {
            let newSqlError: string = SqlServerValidationUtility.validateAzureSQLCreate(this.sqlServer, this.username, this.password, this.passwordConfirmation);
            if (newSqlError) {
                this.MS.ErrorService.message = newSqlError;
            } else {
                let databasesResponse = await this.ValidateAzureServerIsAvailable();
                if ((databasesResponse.IsSuccess)) {
                    this.isValidated = true;
                } else {
                    this.isValidated = false;
                }
            }
        }

        let isInitValid: boolean = await super.OnValidate();
        this.isValidated = this.isValidated && isInitValid;

        return this.isValidated;
    }

    async NavigatingNext(): Promise<boolean> {
        let body = this.GetBody(true);
        let response: ActionResponse = null;

        if (this.sqlInstance === 'ExistingSql') {
            response = await this.MS.HttpService.executeAsync('Microsoft-GetSqlConnectionString', body);
            this.MS.DataStore.addToDataStore('Database', this.database, DataStoreType.Public);
        } else if (this.sqlInstance === 'NewSql') {
            response = await this.CreateDatabaseServer();
            this.MS.DataStore.addToDataStore('Database', this.newSqlDatabase, DataStoreType.Public);
        }

        if (!response || !response.IsSuccess) {
            return false;
        }

        this.MS.DataStore.addToDataStore('SqlConnectionString', response.Body.value, DataStoreType.Private);
        this.MS.DataStore.addToDataStore('Server', this.getSqlServer(), DataStoreType.Public);
        this.MS.DataStore.addToDataStore('Username', this.username, DataStoreType.Public);
        this.MS.DataStore.addToDataStore('Password', this.password, DataStoreType.Private);

        if (this.sqlInstance === 'ExistingSql') {
            this.MS.HttpService.executeAsync('Microsoft-ExistingSqlServer', { isInvisible: true });
        }

        if (this.checkSqlVersion) {
            let responseVersion = await this.MS.HttpService.executeAsync('Microsoft-CheckSQLVersion', body);
            if (!responseVersion.IsSuccess) {
                return false;
            }
        }

        if (this.useImpersonation) {
            this.MS.DataStore.addToDataStore('CredentialTarget', this.credentialTarget, DataStoreType.Private);
            this.MS.DataStore.addToDataStore('CredentialUsername', this.username, DataStoreType.Private);
            this.MS.DataStore.addToDataStore('CredentialPassword', this.password, DataStoreType.Private);
            let responseVersion = await this.MS.HttpService.executeAsync('Microsoft-CredentialManagerWrite', body);
            if (!responseVersion.IsSuccess) {
                return false;
            }
        }

        this.MS.DataStore.addToDataStore('azureSqlDisabled', this.isAzureSql || this.isGovAzureSql ? 'false' : 'true', DataStoreType.Public);

        return true;
    }

    private async GetDatabases(): Promise<ActionResponse> {
        let body: any = this.GetBody(true);
        return this.showAllWriteableDatabases
            ? await this.MS.HttpService.executeAsync('Microsoft-ValidateAndGetWritableDatabases', body)
            : await this.MS.HttpService.executeAsync('Microsoft-ValidateAndGetAllDatabases', body);
    }

    private GetBody(withDatabase: boolean): any {
        let body: any = {};

        body.useImpersonation = this.useImpersonation;
        body['SqlCredentials'] = {};
        body['SqlCredentials']['Server'] = this.getSqlServer();
        body['SqlCredentials']['User'] = this.username;
        body['SqlCredentials']['Password'] = this.password;
        body['SqlCredentials']['AuthType'] = this.isWindowsAuth && !this.isAzureSql ? 'windows' : 'sql';

        if (this.isAzureSql) {
            body['SqlCredentials']['AuthType'] = 'sql';
        }

        if (withDatabase) {
            body['SqlCredentials']['Database'] = this.database;
        }

        return body;
    }

    private getSqlServer(): string {
        let sqlServer: string = this.sqlServer;
        if (this.isAzureSql &&
            this.isGovAzureSql &&
            !sqlServer.includes(this.azureSqlSuffix) &&
            !sqlServer.includes(this.azureSqlSuffix)) {
            sqlServer += this.azureGovtSuffix;
        }
        if (this.isAzureSql && !this.isGovAzureSql && !sqlServer.includes(this.azureSqlSuffix)) {
            sqlServer += this.azureSqlSuffix;
        }
        return sqlServer;
    }

    private async CreateDatabaseServer(): Promise<ActionResponse> {
        this.navigationMessage = this.MS.Translate.SQL_SERVER_CREATING_NEW;
        let body = this.GetBody(true);
        body['SqlCredentials']['Database'] = this.newSqlDatabase;

        this.MS.DataStore.addToDataStore('SqlLocation', this.sqlLocation, DataStoreType.Public);
        this.MS.DataStore.addToDataStore('SqlSku', this.sqlSku, DataStoreType.Public);

        return await this.MS.HttpService.executeAsync('Microsoft-CreateAzureSql', body);
    }

    private async ValidateAzureServerIsAvailable(): Promise<ActionResponse> {
        let body = this.GetBody(false);
        return await this.MS.HttpService.executeAsync('Microsoft-ValidateAzureSqlExists', body);
    }
}