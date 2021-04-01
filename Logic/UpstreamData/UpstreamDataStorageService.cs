using System;
using System.Collections.Generic;
using BraaapWeb.Client;
using maxbl4.Race.Logic.EventModel.Storage.Identifier;
using maxbl4.Race.Logic.EventStorage.Storage.Model;
using maxbl4.Race.Logic.EventStorage.Storage.Traits;
using maxbl4.Race.Logic.ServiceBase;
using Microsoft.Extensions.Options;

namespace maxbl4.Race.Logic.UpstreamData
{
    public class UpstreamDataStorageService: StorageServiceBase
    {
        public UpstreamDataStorageService(IOptions<UpstreamDataSyncServiceOptions> options) : base(options.Value.StorageConnectionString)
        {
        }

        protected override void ValidateDatabase()
        {
        }

        protected override void SetupIndexes()
        {
        }

        public DateTime GetLastSyncTimestamp()
        {
            return repo.Query<Timestamp>().FirstOrDefault()?.LastSyncTimeStamp ?? Constants.DefaultUtcDate;
        }

        public void PurgeExistingData()
        {
            repo.DeleteMany<SeriesDto>(x => true);
            repo.DeleteMany<ChampionshipDto>(x => true);
            repo.DeleteMany<ClassDto>(x => true);
            repo.DeleteMany<EventDto>(x => true);
            repo.DeleteMany<EventConfirmation>(x => true);
            repo.DeleteMany<ScheduleItemDto>(x => true);
            repo.DeleteMany<ScheduleToClass>(x => true);
            repo.DeleteMany<RiderProfile>(x => true);
            repo.DeleteMany<RiderRegistration>(x => true);
        }

        public void UpsertSeries(IEnumerable<SeriesDto> entities)
        {
            repo.Upsert(entities.ApplyTraits());
        }

        public void UpsertChampionships(IEnumerable<ChampionshipDto> entities)
        {
            repo.Upsert(entities.ApplyTraits());
        }

        public void UpsertClasses(IEnumerable<ClassDto> entities)
        {
            repo.Upsert(entities.ApplyTraits());
        }

        public void UpsertEvents(IEnumerable<EventDto> entities)
        {
            repo.Upsert(entities.ApplyTraits());
        }

        public void UpsertEventConfirmations(IEnumerable<EventConfirmation> entities)
        {
            repo.Upsert(entities);
        }

        public void UpsertSchedules(IEnumerable<ScheduleItemDto> entities)
        {
            repo.Upsert(entities.ApplyTraits());
        }

        public void UpsertScheduleToClasses(IEnumerable<ScheduleToClass> entities)
        {
            repo.Upsert(entities);
        }

        public void UpsertRiderProfiles(IEnumerable<RiderProfileDto> entities)
        {
            repo.Upsert(entities.ApplyTraits());
        }

        public void UpsertRiderRegistrations(IEnumerable<RiderRegistration> entities)
        {
            repo.Upsert(entities);
        }

        public IEnumerable<SeriesDto> ListSeries()
        {
            return repo.Query<SeriesDto>().ToEnumerable();
        }
        
        public IEnumerable<ChampionshipDto> ListChampionships(Id<SeriesDto>? seriesId = null)
        {
            var query = repo.Query<ChampionshipDto>();
            if (seriesId != null && seriesId != Id<SeriesDto>.Empty)
                query = query.Where(x => x.SeriesId == seriesId);
            return query.ToEnumerable();
        }
        
        public IEnumerable<ClassDto> ListClasses(Id<ChampionshipDto>? championshipId = null)
        {
            var query = repo.Query<ClassDto>();
            if (championshipId != null && championshipId != Id<ChampionshipDto>.Empty)
                query = query.Where(x => x.ChampionshipId == championshipId);
            return query.ToEnumerable();
        }
        
        public IEnumerable<EventDto> ListEvents(Id<ChampionshipDto>? championshipId = null)
        {
            var query = repo.Query<EventDto>();
            if (championshipId != null && championshipId != Id<ChampionshipDto>.Empty)
                query = query.Where(x => x.ChampionshipId == championshipId);
            return query.ToEnumerable();
        }

        private class Timestamp
        {
            public int Id { get; set; } = 1;
            public DateTime LastSyncTimeStamp { get; set; } = Constants.DefaultUtcDate;
        }
    }
}