using DV.Logic.Job;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JobGenCompatLayer.Builders
{
    public class EmptyHaulDefinitionBuilder : JobDefinitionBuilder
    {
        private StationController origin;
        private StationController destination;
        private Track start;
        private Track end;
        private List<TrainCarType> carTypes;

        private float TrainLength
        {
            get
            {
                if (carTypes.Count == 0) { return 0f; }
                var yto = SingletonBehaviour<YardTracksOrganizer>.Instance;
                return yto.GetTotalCarTypesLength(carTypes) + yto.GetSeparationLengthBetweenCars(carTypes.Count);
            }
        }
        private float BonusTimeLimit
        {
            get
            {
                float distanceBetweenStations = JobPaymentCalculator.GetDistanceBetweenStations(origin, destination);
                return JobPaymentCalculator.CalculateHaulBonusTimeLimit(distanceBetweenStations);
            }
        }
        private float InitialWage
        {
            get
            {
                float distanceBetweenStations = JobPaymentCalculator.GetDistanceBetweenStations(origin, destination);
                return JobPaymentCalculator.CalculateJobPayment(JobType.EmptyHaul, distanceBetweenStations, Utils.ExtractPaymentCalculationData(carTypes));
            }
        }
        private JobLicenses RequiredLicenses
        {
            get
            {
                return LicenseManager.GetRequiredLicensesForJobType(JobType.EmptyHaul) |
                    Utils.GetRequiredLicensesForCarTypes(carTypes) |
                    LicenseManager.GetRequiredLicenseForNumberOfTransportedCars(carTypes.Count);
            }
        }

        public override bool IsValid
        {
            get
            {
                return origin != null &&
                    destination != null &&
                    start != null &&
                    end != null &&
                    carTypes != null &&
                    carTypes.Count > 0;
            }
        }

        public override StaticJobDefinition Build(GameObject gameObject, List<Car> spawnedCars)
        {
            if (spawnedCars.Count != carTypes.Count)
            {
                Debug.LogError(() => $"Number of spawned cars ({spawnedCars.Count}) doesn't match number of car types ({carTypes.Count}).");
                return null;
            }
            var chainData = new StationsChainData(origin.stationInfo.YardID, destination.stationInfo.YardID);
            var staticEmptyHaulJobDefinition = gameObject.AddComponent<StaticEmptyHaulJobDefinition>();
            staticEmptyHaulJobDefinition.PopulateBaseJobDefinition(origin.logicStation, BonusTimeLimit, InitialWage, chainData, RequiredLicenses);
            staticEmptyHaulJobDefinition.startingTrack = start;
            staticEmptyHaulJobDefinition.trainCarsToTransport = spawnedCars;
            staticEmptyHaulJobDefinition.destinationTrack = end;
            return staticEmptyHaulJobDefinition;
        }

        public EmptyHaulDefinitionBuilder StartAt(StationController stationController, Track storageTrack)
        {
            origin = stationController;
            start = storageTrack;
            return this;
        }

        public EmptyHaulDefinitionBuilder EndAt(StationController stationController, Track storageTrack)
        {
            destination = stationController;
            end = storageTrack;
            return this;
        }

        public EmptyHaulDefinitionBuilder Couple(params TrainCarType[] carTypes)
        {
            this.carTypes = carTypes.ToList();
            return this;
        }

        public EmptyHaulDefinitionBuilder Haul(IEnumerable<TrainCarType> carTypes)
        {
            return Couple(carTypes.ToArray());
        }
    }
}
