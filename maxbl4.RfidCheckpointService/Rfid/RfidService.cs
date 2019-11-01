﻿using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using maxbl4.RaceLogic.Checkpoints;
using maxbl4.RfidCheckpointService.Services;
using maxbl4.RfidDotNet;
using maxbl4.RfidDotNet.AlienTech.Ext;
using maxbl4.RfidDotNet.GenericSerial.Ext;
using Microsoft.Extensions.Hosting;
using System.Reactive.PlatformServices;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;

namespace maxbl4.RfidCheckpointService.Rfid
{
    public class RfidService : IDisposable
    {
        private readonly StorageService storageService;
        private readonly IMessageHub messageHub;
        private readonly ISystemClock systemClock;
        private readonly ILogger<RfidService> logger;
        private RfidSettings settings;
        private readonly UniversalTagStreamFactory factory;
        private IUniversalTagStream stream;
        private CompositeDisposable disposable;
        private TimestampCheckpointAggregator aggregator;
        
        public RfidService(StorageService storageService, IMessageHub messageHub, 
            ISystemClock systemClock, ILogger<RfidService> logger)
        {
            this.storageService = storageService;
            this.messageHub = messageHub;
            this.systemClock = systemClock;
            this.logger = logger;
            factory = new UniversalTagStreamFactory();
            factory.UseAlienProtocol();
            factory.UseSerialProtocol();
            settings = storageService.GetRfidSettings();
            aggregator = new TimestampCheckpointAggregator(TimeSpan.FromMilliseconds(settings.CheckpointAggregationWindowMs));
            aggregator.Subscribe(OnCheckpoint);
            if (settings.RfidEnabled)
                EnableRfid();
        }

        void OnCheckpoint(Checkpoint cp)
        {
            Safe.Execute(() => messageHub.Publish(cp), logger);
            Safe.Execute(() => storageService.AppendCheckpoint(cp), logger);
        }

        public async Task EnableRfid()
        {
            stream = factory.CreateStream(settings.GetConnectionString());
            disposable = new CompositeDisposable(stream,
                stream.Tags.Select(x => new Checkpoint(x.TagId, systemClock.UtcNow.UtcDateTime)).Subscribe(aggregator));
            await stream.Start();
        }

        public void DisableRfid()
        {
            disposable?.Dispose();
        }

        public void Dispose()
        {
            disposable?.Dispose();
        }
    }
}