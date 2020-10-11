using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xim.Simulators.ServiceBus.Entities;

namespace Xim.Simulators.ServiceBus.Processing
{
    internal class EntityLookup : IEntityLookup
    {
        private readonly Dictionary<string, IEntity> _entities;

        internal EntityLookup(IServiceBusSimulator simulator)
        {
            var topics = simulator.Topics.Values
                .Select(topic => new
                {
                    topic.Name,
                    Entity = (IEntity)topic
                });

            var subscriptions = simulator.Topics.Values
                .SelectMany(topic => topic
                    .Subscriptions
                    .Values
                    .Select(subscription => new
                    {
                        Name = $"{topic.Name}/Subscriptions/{subscription.Name}",
                        Entity = (IEntity)subscription
                    })
                );

            var queues = simulator.Queues.Values
                .Select(queue => new
                {
                    queue.Name,
                    Entity = (IEntity)queue
                });

            _entities = topics
                .Concat(subscriptions)
                .Concat(queues)
                .ToDictionary(
                    item => item.Name,
                    item => item.Entity,
                    StringComparer.OrdinalIgnoreCase
                );
        }

        public IEntity Find(string name)
            => _entities.TryGetValue(name, out IEntity entity)
                ? entity
                : null;

        public IEnumerator<(string Address, IEntity Entity)> GetEnumerator()
        {
            foreach (KeyValuePair<string, IEntity> item in _entities)
                yield return (item.Key, item.Value);
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
