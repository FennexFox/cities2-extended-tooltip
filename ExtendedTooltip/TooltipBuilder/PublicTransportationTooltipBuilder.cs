using Colossal.Entities;

using ExtendedTooltip.Systems;

using Game.Objects;
using Game.Routes;
using Game.UI.Tooltip;

using System.Collections.Generic;
using System.Text;

using Unity.Entities;
using Unity.Mathematics;

namespace ExtendedTooltip.TooltipBuilder
{
	public class PublicTransportationTooltipBuilder : TooltipBuilderBase
	{
		public PublicTransportationTooltipBuilder(EntityManager entityManager, CustomTranslationSystem customTranslationSystem)
		: base(entityManager, customTranslationSystem)
		{
			Mod.Log.Info($"Created PublicTransportationTooltipBuilder.");
		}

		public void Build(Entity selectedEntity, TooltipGroup tooltipGroup)
		{
			var model = Mod.Settings;

			if (model.ShowPublicTransportWaitingPassengers == false && model.ShowPublicTransportWaitingTime == false)
			{
				return;
			}

			GetPassengerInfo(selectedEntity, out var averageWaitingTime, out var waitingPassengers, out var debugPassengerInfo);

			if (model.ShowPublicTransportWaitingPassengers)
			{
				StringTooltip waitingPassengersTooltip = new()
				{
					icon = "Media/Game/Icons/Population.svg",
					value = $"{m_CustomTranslationSystem.GetLocalGameTranslation("SelectedInfoPanel.WAITING_PASSENGERS", "Waiting passengers")}: {waitingPassengers}",
				};
				tooltipGroup.children.Add(waitingPassengersTooltip);
			}

			if (model.ShowPublicTransportWaitingTime && waitingPassengers != 0)
			{
				var rawAverageWaitingTime = averageWaitingTime;
				var unit = "s";
				var tooltipColor = TooltipColor.Success;
				if (averageWaitingTime < 0)
				{
					averageWaitingTime = 0;
				}

				if (averageWaitingTime > 120)
				{
					averageWaitingTime = (int)math.round(averageWaitingTime / 60);
					unit = "m";
					tooltipColor = averageWaitingTime < 60 ? TooltipColor.Success : averageWaitingTime < 120 ? TooltipColor.Warning : TooltipColor.Error;
				}

				StringTooltip averageWaitingTimeTooltip = new()
				{
					icon = "Media/Game/Icons/Fastforward.svg",
					value = $"{m_CustomTranslationSystem.GetTranslation("average_waiting_time", "~ waiting time")}: {averageWaitingTime}{unit}",
					color = tooltipColor,
				};
				tooltipGroup.children.Add(averageWaitingTimeTooltip);

				Mod.Log.Info(
					$"Public transport tooltip debug entity={selectedEntity.Index}:{selectedEntity.Version} " +
					$"displayedAvgWait={averageWaitingTime}{unit} rawAvgWaitSeconds={rawAverageWaitingTime} " +
					$"waitingPassengers={waitingPassengers} sources=[{debugPassengerInfo}]");
			}
		}

		private void GetPassengerInfo(Entity selectedEntity, out int averageWaitingTime, out int waitingPassengers, out string debugPassengerInfo)
		{
			var traversedEntities = new HashSet<Entity>();
			var passengerSources = new HashSet<Entity>();
			// Collect all unique WaitingPassengers sources first so the same stop is not counted
			// multiple times through the selected entity, connected routes, and subobjects.
			CollectPassengerSources(selectedEntity, traversedEntities, passengerSources);

			// Compute the displayed wait time once from the deduplicated sources using a
			// passenger-weighted average, then keep the existing 5-second rounding.
			long totalPassengers = 0;
			long weightedWaitingTime = 0;
			var debugInfoBuilder = new StringBuilder();
			foreach (var sourceEntity in passengerSources)
			{
				if (m_EntityManager.TryGetComponent(sourceEntity, out WaitingPassengers waitingPassengersData) == false)
				{
					continue;
				}

				var passengerCount = math.max(0, waitingPassengersData.m_Count);
				totalPassengers += passengerCount;
				weightedWaitingTime += (long)passengerCount * waitingPassengersData.m_AverageWaitingTime;
				if (debugInfoBuilder.Length > 0)
				{
					debugInfoBuilder.Append("; ");
				}

				debugInfoBuilder.Append($"entity={sourceEntity.Index}:{sourceEntity.Version}");
				debugInfoBuilder.Append($", count={waitingPassengersData.m_Count}");
				debugInfoBuilder.Append($", avgWait={waitingPassengersData.m_AverageWaitingTime}");
			}

			waitingPassengers = totalPassengers > int.MaxValue ? int.MaxValue : (int)totalPassengers;
			averageWaitingTime = 0;
			if (totalPassengers > 0)
			{
				averageWaitingTime = (int)(weightedWaitingTime / totalPassengers);
				averageWaitingTime -= averageWaitingTime % 5;
			}

			debugPassengerInfo = debugInfoBuilder.ToString();
		}

		private void CollectPassengerSources(Entity entity, HashSet<Entity> traversedEntities, HashSet<Entity> passengerSources)
		{
			// Avoid revisiting entities while walking composite buildings/subobjects.
			if (traversedEntities.Add(entity) == false)
			{
				return;
			}

			if (m_EntityManager.HasComponent<WaitingPassengers>(entity))
			{
				passengerSources.Add(entity);
			}

			// Include route waypoints as waiting-passenger sources; HashSet deduplicates overlap.
			if (m_EntityManager.TryGetBuffer(entity, true, out DynamicBuffer<ConnectedRoute> connectedRoutes))
			{
				for (var i = 0; i < connectedRoutes.Length; i++)
				{
					var waypoint = connectedRoutes[i].m_Waypoint;
					if (m_EntityManager.HasComponent<WaitingPassengers>(waypoint))
					{
						passengerSources.Add(waypoint);
					}
				}
			}

			if (m_EntityManager.TryGetBuffer(entity, true, out DynamicBuffer<SubObject> subObjects))
			{
				for (var i = 0; i < subObjects.Length; i++)
				{
					CollectPassengerSources(subObjects[i].m_SubObject, traversedEntities, passengerSources);
				}
			}
		}
	}
}
