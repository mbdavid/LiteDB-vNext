﻿using LiteDB.Engine.Structures.ErrorHandler;

namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task OpenAsync()
    {
        var lockService = _factory.LockService;
        var diskService = _factory.DiskService;
        var logService = _factory.LogService;
        var allocationMapService = _factory.AllocationMapService;
        var masterService = _factory.MasterService;
        var recoveryService = _factory.RecoveryService;

        if (_factory.State != EngineState.Close) throw ERR("must be closed");

        // clean last database exception
        _factory.Exception = null;

        // must run in exclusive mode
        await lockService.EnterExclusiveAsync();

        if (_factory.State != EngineState.Close) throw ERR("must be closed");

        try
        {
            // open/create data file and returns file header
            _factory.FileHeader = await diskService.InitializeAsync();

            // checks if datafile was finish correctly
            if (_factory.FileHeader.IsDirty)
            {
                _factory.State = EngineState.Recovery;

                // do a database recovery
                await recoveryService.DoRecoveryAsync();
            }

            // initialize log service based on disk
            logService.Initialize();

            // initialize AM service
            await allocationMapService.InitializeAsync();

            // read $master
            await masterService.InitializeAsync();

            // update header/state
            _factory.State = EngineState.Open;

            // release exclusive
            lockService.ExitExclusive();

        }
        catch (Exception ex)
        {
            ex.Handle(_factory, true);
        }
    }
}