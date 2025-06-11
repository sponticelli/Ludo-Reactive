namespace Ludo.Reactive.Examples
{
    using System;

    public static class Examples
    {
        public static void BasicUsageExample()
        {
            // Create reactive state
            var counter = ReactiveFlow.CreateState(0);
            var multiplier = ReactiveFlow.CreateState(2);

            // Create computed value
            var doubled = ReactiveFlow.CreateComputed("doubled", builder =>
            {
                return builder.Track(counter) * builder.Track(multiplier);
            });

            // Create effect that runs when computed value changes
            var effect = ReactiveFlow.CreateEffect("logger", builder =>
            {
                var value = builder.Track(doubled);
                Console.WriteLine($"Doubled value: {value}");
            });

            // Update state - this will trigger the effect
            counter.Set(5); // Prints: "Doubled value: 10"
            multiplier.Set(3); // Prints: "Doubled value: 15"

            // Cleanup
            effect.Dispose();
            doubled.Dispose();
        }

        public static void DynamicManagerExample()
        {
            var manager = ReactiveFlow.CreateDynamicManager();
            var items = ReactiveFlow.CreateState(new[] { "a", "b", "c" });

            var listEffect = ReactiveFlow.CreateEffect("list-manager", builder =>
            {
                var currentItems = builder.Track(items);
                
                foreach (var item in currentItems)
                {
                    manager.ManageEffect(item, itemBuilder =>
                    {
                        Console.WriteLine($"Processing item: {item}");
                    });
                }
            });

            // Update items - old effects are automatically cleaned up
            items.Set(new[] { "x", "y" });

            // Cleanup
            listEffect.Dispose();
            manager.Dispose();
        }
    }
}