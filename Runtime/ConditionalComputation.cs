using System;
using Ludo.Reactive.ErrorHandling;

namespace Ludo.Reactive
{
    /// <summary>
    /// Computation that only executes when a condition is met
    /// </summary>
    public class ConditionalComputation : ReactiveComputation
    {
        private readonly IReadOnlyReactiveValue<bool> _condition;
        private readonly Action<ComputationBuilder> _conditionalLogic;

        public ConditionalComputation(
            string name,
            ReactiveScheduler scheduler,
            IReadOnlyReactiveValue<bool> condition,
            Action<ComputationBuilder> conditionalLogic,
            ErrorBoundary errorBoundary = null,
            params IObservable[] staticDependencies)
            : base(name, scheduler, errorBoundary)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
            _conditionalLogic = conditionalLogic ?? throw new ArgumentNullException(nameof(conditionalLogic));
            
            AddStaticDependency(condition);
            
            foreach (var dependency in staticDependencies ?? new IObservable[0])
            {
                AddStaticDependency(dependency);
            }
            
            ScheduleExecution();
        }

        protected override void ExecuteComputation()
        {
            using var builder = new ComputationBuilder(this);
            if (builder.Track(_condition))
            {
                _conditionalLogic(builder);
            }
        }
    }
}