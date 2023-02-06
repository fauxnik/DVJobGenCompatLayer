using DV.Logic.Job;
using System.Collections.Generic;
using System.Linq;
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

        public TransportDefinitionBuilder StartAt(StationController stationController, Track outboundTrack)
        {
            origin = stationController;
            outbound = outboundTrack;
            return this;
        }

        public TransportDefinitionBuilder EndAt(StationController stationController, Track inboundTrack)
        {
            destination = stationController;
            inbound = inboundTrack;
            return this;
        }

        public TransportDefinitionBuilder Couple(params TrainCarType[] carTypes)
        {
            this.carTypes = carTypes.ToList();
            return this;
        }

        public TransportDefinitionBuilder Carrying(params CargoType[] cargoTypes)
        {
            this.cargoTypes = cargoTypes.ToList();
            return this;
        }

        public TransportDefinitionBuilder OfQuantity(params float[] cargoAmounts)
        {
            this.cargoAmounts = cargoAmounts.ToList();
            return this;
        }

        public TransportDefinitionBuilder Haul(IEnumerable<TrainCarType> carTypes, IEnumerable<CargoType> cargoTypes, IEnumerable<float> cargoAmounts)
        {
            return Couple(carTypes.ToArray()).Carrying(cargoTypes.ToArray()).OfQuantity(cargoAmounts.ToArray());
        }
    }
}
