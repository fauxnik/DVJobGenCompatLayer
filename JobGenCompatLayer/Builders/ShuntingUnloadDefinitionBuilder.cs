using DV.Logic.Job;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JobGenCompatLayer.Builders
{
    public class ShuntingUnloadDefinitionBuilder : JobDefinitionBuilder
    {
        private StationController origin;
        private StationController destination;
        private Track inbound;
        private WarehouseMachine warehouse;
        private List<TrainCarType> carTypes;
        private List<CargoType> cargoTypes;
        private List<float> cargoAmounts;
        private Dictionary<Track, List<int>> carTypeIndicesPerEndTrack;
        
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
                return JobPaymentCalculator.CalculateShuntingBonusTimeLimit(carTypeIndicesPerEndTrack.Count);
            }
        }
        private float InitialWage
        {
            get
            {
                float distanceInMeters = 500f * carTypeIndicesPerEndTrack.Count;
                return JobPaymentCalculator.CalculateJobPayment(JobType.ShuntingUnload, distanceInMeters, Utils.ExtractPaymentCalculationData(carTypes, cargoTypes));
            }
        }
        private JobLicenses RequiredLicenses
        {
            get
            {
                return LicenseManager.GetRequiredLicensesForJobType(JobType.ShuntingUnload) |
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
                    inbound != null &&
                    carTypes != null &&
                    carTypes.Count > 0 &&
                    cargoTypes != null &&
                    cargoTypes.Count == carTypes.Count &&
                    cargoAmounts != null &&
                    cargoAmounts.Count == carTypes.Count &&
                    carTypeIndicesPerEndTrack != null &&
                    carTypeIndicesPerEndTrack.Aggregate(0, (sum, pair) => sum + pair.Value.Count) == carTypes.Count &&
                    carTypeIndicesPerEndTrack.Aggregate(true, (isInRange, pair) => isInRange && pair.Value.TrueForAll(index => index < carTypes.Count));
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
            var staticShuntingUnloadJobDefinition = gameObject.AddComponent<StaticShuntingUnloadJobDefinition>();
            staticShuntingUnloadJobDefinition.PopulateBaseJobDefinition(destination.logicStation, BonusTimeLimit, InitialWage, chainData, RequiredLicenses);
            staticShuntingUnloadJobDefinition.startingTrack = inbound;
            staticShuntingUnloadJobDefinition.unloadMachine = warehouse;
            var unloadData = Utils.BuildWarehouseData(spawnedCars, cargoTypes, cargoAmounts);
            if (unloadData == null)
            {
                Debug.LogError(() => $"Failed to build unload data.");
                return null;
            }
            staticShuntingUnloadJobDefinition.unloadData = unloadData;
            var carsPerEndTrack = Utils.GroupCarsByTrack(spawnedCars, carTypeIndicesPerEndTrack);
            if (carsPerEndTrack == null)
            {
                Debug.LogError(() => $"Failed to group cars by end track.");
                return null;
            }
            staticShuntingUnloadJobDefinition.carsPerDestinationTrack = carsPerEndTrack;
            staticShuntingUnloadJobDefinition.forceCorrectCargoStateOnCars = true;
            return staticShuntingUnloadJobDefinition;
        }

        public ShuntingUnloadDefinitionBuilder StartAt(StationController stationController, Track inboundTrack)
        {
            origin = stationController;
            inbound = inboundTrack;
            return this;
        }

        public ShuntingUnloadDefinitionBuilder Couple(params TrainCarType[] carTypes)
        {
            this.carTypes = carTypes.ToList();
            return this;
        }

        public ShuntingUnloadDefinitionBuilder Unload(WarehouseMachine warehouseMachine, params CargoType[] cargoTypes)
        {
            warehouse = warehouseMachine;
            this.cargoTypes = cargoTypes.ToList();
            return this;
        }

        public ShuntingUnloadDefinitionBuilder OfQuantity(params float[] cargoAmounts)
        {
            this.cargoAmounts = cargoAmounts.ToList();
            return this;
        }

        public ShuntingUnloadDefinitionBuilder EndAt(StationController stationController)
        {
            destination = stationController;
            return this;
        }

        public ShuntingUnloadDefinitionBuilder OnTracks(Dictionary<Track, List<int>> indicesOfCarTypesPerEndTrack)
        {
            carTypeIndicesPerEndTrack = indicesOfCarTypesPerEndTrack;
            return this;
        }

        public ShuntingUnloadDefinitionBuilder Shunt(IEnumerable<TrainCarType> carTypes, IEnumerable<CargoType> cargoTypes, IEnumerable<float> cargoAmounts, WarehouseMachine warehouseMachine)
        {
            return Couple(carTypes.ToArray()).Unload(warehouseMachine, cargoTypes.ToArray()).OfQuantity(cargoAmounts.ToArray());
        }
    }
}
