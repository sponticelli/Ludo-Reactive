using System;
using Ludo.Reactive.ErrorHandling;

namespace Ludo.Reactive
{
    /// <summary>
    /// Side-effect computation that runs when dependencies change
    /// </summary>
    public class ReactiveEffect : ReactiveComputation
    {
        private readonly Action<ComputationBuilder> _computationLogic;

        public ReactiveEffect(
            string name,
            ReactiveScheduler scheduler,
            Action<ComputationBuilder> logic,
            ErrorBoundary errorBoundary = null,
            params IObservable[] staticDependencies)
            : base(name, scheduler, errorBoundary)
        {
            _computationLogic = logic ?? throw new ArgumentNullException(nameof(logic));

            foreach (var dependency in staticDependencies ?? new IObservable[0])
            {
                AddStaticDependency(dependency);
            }

            ScheduleExecution();
        }

        protected override void ExecuteComputation()
        {
            using var builder = new ComputationBuilder(this);
            _computationLogic(builder);
        }
    }
}