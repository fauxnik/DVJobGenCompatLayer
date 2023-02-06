using DV.Logic.Job;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JobGenCompatLayer.Builders
{
    public class JobChainBuilder
    {
        public JobChainBuilder(JobType firstJobTypeInChain, StationController stationControllerTriggeringJobGeneration)
        {
            startingJobType = firstJobTypeInChain;
            localStation = stationControllerTriggeringJobGeneration;
        }

        private List<JobDefinitionBuilder> jobDefinitionBuilders = new List<JobDefinitionBuilder>();
        private JobType startingJobType;
        private StationController localStation;
        private StationController origin;
        private StationController destination;

        public bool IsValid
        {
            get
            {
                return jobDefinitionBuilders.Count > 0 &&
                    jobDefinitionBuilders.TrueForAll(jdb => jdb.IsValid) &&
                    localStation != null &&
                    origin != null &&
                    destination != null;
            }
        }

        public JobChainController Build(List<Car> spawnedCars)
        {
            var gameObject = new GameObject($"ChainJob[{startingJobType}]: {origin.logicStation.ID} - {destination.logicStation.ID}");
            gameObject.transform.SetParent(localStation.transform);

            var CreateJobChain = jobChainCreatorPerStartingJobType[startingJobType];
            if (CreateJobChain == null)
            {
                Debug.LogError(() => $"Missing job chain creator for job type {startingJobType}");
                return null;
            }

            var jobChainController = CreateJobChain(gameObject);

            for (var i = 0; i < jobDefinitionBuilders.Count; ++i)
            {
                var jdb = jobDefinitionBuilders[i];
                var staticJobDefinition = jdb.Build(gameObject, spawnedCars);
                if (staticJobDefinition == null)
                {
                    Debug.LogError(() => $"Failed to build job definition {i + 1} (of {jobDefinitionBuilders.Count}) for starting job type {startingJobType}.");
                    jobChainController.DestroyChain();
                    return null;
                }
                jobChainController.AddJobDefinitionToChain(staticJobDefinition);
            }

            jobChainController.FinalizeSetupAndGenerateFirstJob(false);
            return jobChainController;
        }

        private static Dictionary<JobType, Func<GameObject, JobChainController>> jobChainCreatorPerStartingJobType = new Dictionary<JobType, Func<GameObject, JobChainController>>
        {
            { JobType.ShuntingLoad, gameObject => new JobChainControllerWithEmptyHaulGeneration(gameObject) },
            { JobType.ShuntingUnload, gameObject => new JobChainControllerWithEmptyHaulGeneration(gameObject) },
            { JobType.Transport, gameObject => new JobChainControllerWithEmptyHaulGeneration(gameObject) },
            { JobType.EmptyHaul, gameObject => new JobChainController(gameObject) },
        };

        /**
         * TODO: document public API method
         */
        public static void RegisterJobChainCreatorForJobType(JobType jobType, Func<GameObject, JobChainController> jobChainCreator)
        {
            if (jobChainCreator == null) { throw new ArgumentNullException($"JobChainBuilder.RegisterJobChainCreatorForJobType requires the jobChainCreator argument, but none was passed."); }

            if (jobChainCreatorPerStartingJobType.ContainsKey(jobType)) { Debug.LogWarning(() => $"Overwriting already established job chain creator for job type {jobType}."); }

            jobChainCreatorPerStartingJobType[jobType] = jobChainCreator;
        }

        /**
         * TODO: document public API method
         */
        public JobChainBuilder StartAt(StationController originStationController)
        {
            if (origin != null) { Debug.LogWarning(() => $"Overwriting already added origin station controller. Was: {origin.logicStation.ID} Now: {originStationController.logicStation.ID}"); }
            origin = originStationController;
            return this;
        }

        /**
         * TODO: document public API method
         */
        public JobChainBuilder EndAt(StationController destinationStationController)
        {
            if (destination != null) { Debug.LogWarning(() => $"Overwriting already added destination station controller. Was: {destination.logicStation.ID} Now: {destinationStationController.logicStation.ID}"); }
            destination = destinationStationController;
            return this;
        }

        /**
         * TODO: document public API method
         */
        public JobChainBuilder Then(JobDefinitionBuilder jobDefinitionBuilder)
        {
            jobDefinitionBuilders.Add(jobDefinitionBuilder);
            return this;
        }

        /**
         * TODO: document public API method
         */
        public JobChainBuilder DoJobs(IEnumerable<JobDefinitionBuilder> jobDefinitionBuilders)
        {
            this.jobDefinitionBuilders.AddRange(jobDefinitionBuilders);
            return this;
        }
    }
}
