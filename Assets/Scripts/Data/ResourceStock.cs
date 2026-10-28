using System.Collections.Generic;

namespace EpochCity
{
    /// <summary>Warenvorrat: wie viel von jeder Ressource ist vorhanden?</summary>
    public class ResourceStock
    {
        private readonly Dictionary<ResourceDefinition, float> amounts = new Dictionary<ResourceDefinition, float>();

        public IReadOnlyDictionary<ResourceDefinition, float> All => amounts;

        public float Get(ResourceDefinition resource) =>
            resource != null && amounts.TryGetValue(resource, out var value) ? value : 0f;

        public void Add(ResourceDefinition resource, float amount)
        {
            if (resource == null) return;
            amounts[resource] = Get(resource) + amount;
        }
    }
}
