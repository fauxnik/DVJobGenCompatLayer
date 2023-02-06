using DV.Logic.Job;
using System.Collections.Generic;
using UnityEngine;

namespace JobGenCompatLayer.Builders
{
    public class TransportDefinitionBuilder : JobDefinitionBuilder
    {
        private StationController origin;
        private StationController destination;
        private Track outbound;
        private Track inbound;
        private List<TrainCarType> carTypes;
        private List<CargoType> cargoTypes;
        private List<float> cargoAmounts;

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
                return JobPaymentCalculator.CalculateJobPayment(JobType.Transport, distanceBetweenStations, Utils.ExtractPaymentCalculationData(carTypes, cargoTypes));
            }
        }
        private JobLicenses RequiredLicenses
        {
            get
            {
                return LicenseManager.GetRequiredLicensesForJobType(JobType.Transport) |
                    LicenseManager.GetRequiredLicensesForCargoTypes(cargoTypes) |
                    LicenseManager.GetRequiredLicenseForNumberOfTransportedCars(carTypes.Count);
            }
        }

        public override bool IsValid
        {
            get
            {
                return origin != null &&
                    destination != null &&
                    outbound != null &&
                    inbound != null &&
                    carTypes != null &&
                    carTypes.Count > 0 &&
                    cargoTypes != null &&
                    cargoTypes.Count == carTypes.Count &&
                    cargoAmounts != null &&
                    cargoAmounts.Count == carTypes.Count;
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
            var staticTransportJobDefinition = gameObject.AddComponent<StaticTransportJobDefinition>();
            staticTransportJobDefinition.PopulateBaseJobDefinition(origin.logicStation, BonusTimeLimit, InitialWage, chainData, RequiredLicenses);
            staticTransportJobDefinition.startingTrack = outbound;
            staticTransportJobDefinition.trainCarsToTransport = spawnedCars;
            staticTransportJobDefinition.transportedCargoPerCar = cargoTypes;
            staticTransportJobDefinition.cargoAmountPerCar = cargoAmounts;
            staticTransportJobDefinition.destinationTrack = inbound;
            staticTransportJobDefinition.forceCorrectCargoStateOnCars = true;
            return staticTransportJobDefinition;
        }
    }
}
