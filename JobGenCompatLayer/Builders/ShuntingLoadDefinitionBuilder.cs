using DV.Logic.Job;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JobGenCompatLayer.Builders
{
    public class ShuntingLoadDefinitionBuilder : JobDefinitionBuilder
    {
        private StationController origin;
        private StationController destination;
        private Track outbound;
        private WarehouseMachine warehouse;
        private List<TrainCarType> carTypes;
        private List<CargoType> cargoTypes;
        private List<float> cargoAmounts;
        private Dictionary<Track, List<int>> carTypeIndicesPerStartTrack;

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
                return JobPaymentCalculator.CalculateShuntingBonusTimeLimit(carTypeIndicesPerStartTrack.Count);
            }
        }
        private float InitialWage
        {
            get
            {
                float distanceInMeters = 500f * carTypeIndicesPerStartTrack.Count;
                return JobPaymentCalculator.CalculateJobPayment(JobType.ShuntingLoad, distanceInMeters, Utils.ExtractPaymentCalculationData(carTypes, cargoTypes));
            }
        }
        private JobLicenses RequiredLicenses
        {
            get
            {
                return LicenseManager.GetRequiredLicensesForJobType(JobType.ShuntingLoad) |
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
                    carTypes != null &&
                    carTypes.Count > 0 &&
                    cargoTypes != null &&
                    cargoTypes.Count == carTypes.Count &&
                    cargoAmounts != null &&
                    cargoAmounts.Count == carTypes.Count &&
                    carTypeIndicesPerStartTrack != null &&
                    carTypeIndicesPerStartTrack.Aggregate(0, (sum, pair) => sum + pair.Value.Count) == carTypes.Count &&
                    carTypeIndicesPerStartTrack.Aggregate(true, (isInRange, pair) => isInRange && pair.Value.TrueForAll(index => index < carTypes.Count));
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
            var staticShuntingLoadJobDefinition = gameObject.AddComponent<StaticShuntingLoadJobDefinition>();
            staticShuntingLoadJobDefinition.PopulateBaseJobDefinition(origin.logicStation, BonusTimeLimit, InitialWage, chainData, RequiredLicenses);
            var carsPerStartTrack = Utils.GroupCarsByTrack(spawnedCars, carTypeIndicesPerStartTrack);
            if (carsPerStartTrack == null)
            {
                Debug.LogError(() => $"Failed to group cars by start track.");
                return null;
            }
            staticShuntingLoadJobDefinition.carsPerStartingTrack = carsPerStartTrack;
            staticShuntingLoadJobDefinition.loadMachine = warehouse;
            var loadData = Utils.BuildWarehouseData(spawnedCars, cargoTypes, cargoAmounts);
            if (loadData == null)
            {
                Debug.LogError(() => $"Failed to build load data.");
                return null;
            }
            staticShuntingLoadJobDefinition.loadData = loadData;
            staticShuntingLoadJobDefinition.destinationTrack = outbound;
            staticShuntingLoadJobDefinition.forceCorrectCargoStateOnCars = true;
            return staticShuntingLoadJobDefinition;
        }

        public ShuntingLoadDefinitionBuilder StartAt(StationController stationController)
        {
            origin = stationController;
            return this;
        }

        public ShuntingLoadDefinitionBuilder EndAt(StationController stationController, Track outboundTrack)
        {
            destination = stationController;
            outbound = outboundTrack;
            return this;
        }

        public ShuntingLoadDefinitionBuilder Couple(params TrainCarType[] carTypes)
        {
            this.carTypes = carTypes.ToList();
            return this;
        }

        public ShuntingLoadDefinitionBuilder OnTracks(Dictionary<Track, List<int>> indicesOfCarTypesPerStartTrack)
        {
            carTypeIndicesPerStartTrack = indicesOfCarTypesPerStartTrack;
            return this;
        }

        public ShuntingLoadDefinitionBuilder Load(WarehouseMachine warehouseMachine, params CargoType[] cargoTypes)
        {
            warehouse = warehouseMachine;
            this.cargoTypes = cargoTypes.ToList();
            return this;
        }

        public ShuntingLoadDefinitionBuilder OfQuantity(params float[] cargoAmounts)
        {
            this.cargoAmounts = cargoAmounts.ToList();
            return this;
        }

        public ShuntingLoadDefinitionBuilder Shunt(IEnumerable<TrainCarType> carTypes, IEnumerable<CargoType> cargoTypes, IEnumerable<float> cargoAmounts, WarehouseMachine warehouseMachine)
        {
            return Couple(carTypes.ToArray()).Load(warehouseMachine, cargoTypes.ToArray()).OfQuantity(cargoAmounts.ToArray());
        }
    }
}
