namespace Mouth.Utils
{
    /// <summary>
    /// Container class that wraps a _value and ensures initialisation is 
    /// called just before first use.
    /// </summary>
    public class LazyValue<T>
    {
        private T _value;
        private bool initialized;
        private InitializerDelegate initializer;

        public delegate T InitializerDelegate();

        /// <summary>
        /// Setup the container but don't initialise the _value yet.
        /// </summary>
        /// <param name="initializer"> 
        /// The initializer delegate to call when first used. 
        /// </param>
        public LazyValue(InitializerDelegate initializer)
        {
            this.initializer = initializer;
        }

        /// <summary>
        /// Get or set the contents of this container.
        /// </summary>
        /// <remarks>
        /// Note that setting the _value before initialisation will initialise 
        /// the class.
        /// </remarks>
        public T value
        {
            get
            {
                // Ensure we init before returning a _value.
                ForceInit();
                return _value;
            }
            set
            {
                // Don't use default init anymore.
                initialized = true;
                _value = value;
            }
        }

        /// <summary>
        /// Force the initialisation of the _value via the delegate.
        /// </summary>
        public void ForceInit()
        {
            if (initialized) return;
            
            _value = initializer();
            initialized = true;
        }
    }
}