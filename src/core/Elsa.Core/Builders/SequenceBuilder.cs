using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Containers;
using Elsa.Activities.Flowcharts;
using Elsa.Activities.Primitives;
using Elsa.Services;

namespace Elsa.Builders
{
    public class SequenceBuilder : IActivityBuilder
    {
        private readonly IActivityResolver activityResolver;
        
        public SequenceBuilder(IActivityResolver activityResolver, IServiceProvider serviceProvider)
        {
            this.activityResolver = activityResolver;
            ServiceProvider = serviceProvider;
            Sequence = activityResolver.ResolveActivity<Sequence>();
        }
        
        public IServiceProvider ServiceProvider { get; }
        public Sequence Sequence { get; }
        
        public SequenceBuilder Add<T>(Action<T>? setup = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity(setup);
            return Add(activity);
        }
        
        public SequenceBuilder Add<T>(T activity) where T : class, IActivity
        {
            Sequence.Activities.Add(activity);
            return this;
        }
        
        public SequenceBuilder Add(Func<SequenceBuilder, IActivityBuilder> activityBuilder)
        {
            var activity = activityBuilder(this).Build(); 
            return Add(activity);
        }

        public SequenceBuilder Add(IEnumerable<IActivity> activities)
        {
            foreach (var activity in activities) 
                Add(activity);
            
            return this;
        }

        
        public IActivity Build() => Sequence;
    }
}