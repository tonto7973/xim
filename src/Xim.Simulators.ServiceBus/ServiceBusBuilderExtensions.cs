using System.Linq;

namespace Xim.Simulators.ServiceBus
{
    /// <summary>
    /// Extension methods for <see cref="ServiceBusBuilder"/>.
    /// </summary>
    public static class ServiceBusBuilderExtensions
    {
        /// <summary>
        /// Registers new <see cref="ServiceBusBuilder"/> with the <see cref="ISimulation"/>.
        /// </summary>
        /// <param name="simulation">The simulation.</param>
        /// <returns>New instance of <see cref="ServiceBusBuilder"/>.</returns>
        /// <exception cref="System.ArgumentNullException">If <paramref name="simulation"/> is null.</exception>
        public static ServiceBusBuilder AddServiceBus(this ISimulation simulation)
            => new ServiceBusBuilder(simulation);

        /// <summary>
        /// Adds a topic to the service bus builder.
        /// </summary>
        /// <param name="serviceBusBuilder">The <see cref="ServiceBusBuilder"/> instance.</param>
        /// <param name="topicName">A topic name.</param>
        /// <param name="subscriptions">A string array that contains zero or more subscription names.</param>
        /// <returns>The <see cref="ServiceBusBuilder"/>.</returns>
        /// <exception cref="System.ArgumentNullException">If <paramref name="topicName"/> is null.</exception>
        /// <exception cref="System.ArgumentException">If <paramref name="topicName"/> or <paramref name="subscriptions"/> are invalid.</exception>
        public static ServiceBusBuilder AddTopic(this ServiceBusBuilder serviceBusBuilder,
            string topicName, params string[] subscriptions)
            => serviceBusBuilder.AddTopic(new Topic(
                    topicName,
                    subscriptions?.Select(name => new Subscription(name)).ToArray()
               ));

        /// <summary>
        /// Adds a queue to the service bus builder.
        /// </summary>
        /// <param name="serviceBusBuilder">The <see cref="ServiceBusBuilder"/> instance.</param>
        /// <param name="queueName">A queue name.</param>
        /// <returns>The <see cref="ServiceBusBuilder"/>.</returns>
        public static ServiceBusBuilder AddQueue(this ServiceBusBuilder serviceBusBuilder,
            string queueName)
            => serviceBusBuilder.AddQueue(new Queue(queueName));
    }
}
