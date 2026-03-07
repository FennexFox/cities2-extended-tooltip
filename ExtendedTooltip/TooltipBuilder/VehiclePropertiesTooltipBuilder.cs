using Colossal.Entities;
using ExtendedTooltip.Systems;
using Game.Prefabs;
using Game.UI.Tooltip;
using Unity.Entities;
using UnityEngine;

namespace ExtendedTooltip.TooltipBuilder
{
	public class VehiclePropertiesTooltipBuilder : TooltipBuilderBase
	{
		PrefabSystem prefabSystem => m_EntityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		public VehiclePropertiesTooltipBuilder(EntityManager entityManager, CustomTranslationSystem customTranslationSystem)
		: base(entityManager, customTranslationSystem)
		{
			Mod.Log.Info($"Created VehiclePropertiesTooltipBuilder.");
		}

		public void Build(Entity entity, TooltipGroup tooltipGroup)
		{
			// Get prefab to access CarData
			if (m_EntityManager.TryGetComponent(entity, out PrefabRef prefabRef))
			{
				if (m_EntityManager.TryGetComponent(prefabRef.m_Prefab, out CarData data))
				{
					var speedTooltip = new StringTooltip
					{
						// Electricity.svg
						icon = "Media/Game/Icons/AdvancedElectricity.svg",
						value = $"MaxSpeed: {UnitHelper.FormatSpeedLimit(data.m_MaxSpeed)}, Acceleration: {data.m_Acceleration}, Braking: {data.m_Braking}"
					};
					tooltipGroup.children.Add(speedTooltip);
				}
				// if (prefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefab))
				// {
				//
				// }
			}

			
		}

		/// <summary>
		/// Returns raw speed
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		private float ConvertSpeed(float ms)
		{
			return ms * 3.6f; // m/s to km/h
		}
	}
}
