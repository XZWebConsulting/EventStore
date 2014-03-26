using System;
using EventStore.Common.Log;
using EventStore.Core.Bus;
using EventStore.Projections.Core.Messages;

namespace EventStore.Projections.Core.Services.Processing
{
    public class ProcessingStrategySelector
    {
        private readonly ILogger _logger = LogManager.GetLoggerFor<ProcessingStrategySelector>();
        private readonly SpooledStreamReadingDispatcher _spoolProcessingResponseDispatcher;
        private readonly ReaderSubscriptionDispatcher _subscriptionDispatcher;

        public ProcessingStrategySelector(
            ReaderSubscriptionDispatcher subscriptionDispatcher,
            SpooledStreamReadingDispatcher spoolProcessingResponseDispatcher)
        {
            _subscriptionDispatcher = subscriptionDispatcher;
            _spoolProcessingResponseDispatcher = spoolProcessingResponseDispatcher;
        }

        public ProjectionProcessingStrategy CreateProjectionProcessingStrategy(
            string name,
            ProjectionVersion projectionVersion,
            ProjectionNamesBuilder namesBuilder,
            IQueryDefinition sourceDefinition,
            ProjectionConfig projectionConfig,
            IProjectionStateHandler stateHandler)
        {

            if (!sourceDefinition.DisableParallelismOption && projectionConfig.StopOnEof && sourceDefinition.ByStreams
                && sourceDefinition.DefinesFold && !string.IsNullOrEmpty(sourceDefinition.CatalogStream))
            {
                return new ParallelQueryProcessingStrategy(
                    name,
                    projectionVersion,
                    stateHandler,
                    projectionConfig,
                    sourceDefinition,
                    namesBuilder,
                    _logger,
                    _spoolProcessingResponseDispatcher,
                    _subscriptionDispatcher);
            }

            if (!sourceDefinition.DisableParallelismOption && projectionConfig.StopOnEof && sourceDefinition.ByStreams
                && sourceDefinition.DefinesFold && sourceDefinition.HasCategories())
            {
                return new ParallelQueryProcessingStrategy(
                    name,
                    projectionVersion,
                    stateHandler,
                    projectionConfig,
                    sourceDefinition,
                    namesBuilder,
                    _logger,
                    _spoolProcessingResponseDispatcher,
                    _subscriptionDispatcher);
            }

            return projectionConfig.StopOnEof
                ? (ProjectionProcessingStrategy)
                    new QueryProcessingStrategy(
                        name,
                        projectionVersion,
                        stateHandler,
                        projectionConfig,
                        sourceDefinition,
                        _logger,
                        _subscriptionDispatcher)
                : new ContinuousProjectionProcessingStrategy(
                    name,
                    projectionVersion,
                    stateHandler,
                    projectionConfig,
                    sourceDefinition,
                    _logger,
                    _subscriptionDispatcher);
        }

        public ProjectionProcessingStrategy CreateSlaveProjectionProcessingStrategy(
            string name, ProjectionVersion projectionVersion, ProjectionSourceDefinition sourceDefinition,
            ProjectionConfig projectionConfig, IProjectionStateHandler stateHandler, IPublisher resultsEnvelope,
            Guid masterCoreProjectionId, ProjectionCoreService projectionCoreService)
        {
            return new SlaveQueryProcessingStrategy(
                name, projectionVersion, stateHandler, projectionConfig, sourceDefinition, projectionCoreService.Logger,
                resultsEnvelope, masterCoreProjectionId, _subscriptionDispatcher);
        }
    }
}
