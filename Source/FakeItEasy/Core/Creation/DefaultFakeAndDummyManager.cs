namespace FakeItEasy.Core.Creation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// The default implementation of the IFakeAndDummyManager interface.
    /// </summary>
    internal class DefaultFakeAndDummyManager
        : IFakeAndDummyManager
    {
        private IFakeObjectContainer container;
        private IProxyGeneratorNew proxyGenerator;
        private FakeObject.Factory fakeObjectFactory;
        private IFakeWrapperConfigurator wrapperConfigurator;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultFakeAndDummyManager"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="proxyGenerator">The proxy generator.</param>
        /// <param name="fakeObjectFactory">The fake object factory.</param>
        public DefaultFakeAndDummyManager(IFakeObjectContainer container, IProxyGeneratorNew proxyGenerator, FakeObject.Factory fakeObjectFactory, IFakeWrapperConfigurator wrapperConfigurator)
        {
            this.container = container;
            this.proxyGenerator = proxyGenerator;
            this.fakeObjectFactory = fakeObjectFactory;
            this.wrapperConfigurator = wrapperConfigurator;
        }

        /// <summary>
        /// Creates a dummy of the specified type.
        /// </summary>
        /// <param name="typeOfDummy">The type of dummy to create.</param>
        /// <returns>The created dummy.</returns>
        /// <exception cref="FakeCreationException">The current IProxyGenerator is not able to generate a fake of the specified type and
        /// the current IFakeObjectContainer does not contain the specified type.</exception>
        public object CreateDummy(Type typeOfDummy)
        {
            object result = null;
            
            if (!this.container.TryCreateFakeObject(typeOfDummy, out result))
            {
                return this.CreateProxy(typeOfDummy, null, true);
            }

            return result;
        }

        /// <summary>
        /// Creates a fake object of the specified type.
        /// </summary>
        /// <param name="typeOfFake">The type of fake object to generate.</param>
        /// <param name="options">Options for building the fake object.</param>
        /// <returns>A fake object.</returns>
        /// <exception cref="FakeCreationException">The current IProxyGenerator is not able to generate a fake of the specified type.</exception>
        public object CreateFake(Type typeOfFake, FakeOptions options)
        {
            var result = this.CreateProxy(typeOfFake, options.ArgumentsForConstructor, true);
            this.ConfigureFakeToWrapWhenAppropriate(options, result);
            return result;
        }

        /// <summary>
        /// Tries to create a dummy of the specified type.
        /// </summary>
        /// <param name="typeOfDummy">The type of dummy to create.</param>
        /// <param name="result">Outputs the result dummy when creation is successful.</param>
        /// <returns>
        /// A value indicating whether the creation was successful.
        /// </returns>
        public bool TryCreateDummy(Type typeOfDummy, out object result)
        {
            if (!this.container.TryCreateFakeObject(typeOfDummy, out result))
            {
                result = this.CreateProxy(typeOfDummy, null, false);
            }

            return result != null;
        }

        /// <summary>
        /// Tries to create a fake object of the specified type.
        /// </summary>
        /// <param name="typeOfFake">The type of fake to create.</param>
        /// <param name="options">Options for the creation of the fake.</param>
        /// <param name="result">The created fake object when creation is successful.</param>
        /// <returns>
        /// A value indicating whether the creation was successful.
        /// </returns>
        public bool TryCreateFake(Type typeOfFake, FakeOptions options, out object result)
        {
            result = this.CreateProxy(typeOfFake, options.ArgumentsForConstructor, false);

            this.ConfigureFakeToWrapWhenAppropriate(options, result);

            return result != null;
        }

        /// <summary>
        /// Creates a proxy of the specified type.
        /// </summary>
        /// <param name="typeOfProxy">The type of proxy.</param>
        /// <param name="argumentsForConstructor">The arguments for constructor.</param>
        /// <param name="throwOnFailure">if set to <c>true</c> an exception is thrown when the proxy generator
        /// can not generate a proxy of the specified type.</param>
        /// <returns>A proxy.</returns>
        internal virtual object CreateProxy(Type typeOfProxy, IEnumerable<object> argumentsForConstructor, bool throwOnFailure)
        {
            var fakeObject = this.CreateNewFakeObject();
            
            var proxyResult = this.proxyGenerator.GenerateProxy(typeOfProxy, fakeObject, argumentsForConstructor);

            if (throwOnFailure)
            {
                AssertThatProxyWasSuccessfullyCreated(typeOfProxy, proxyResult);
            }

            fakeObject.SetProxy(proxyResult);

            return proxyResult.Proxy;
        }

        private void ConfigureFakeToWrapWhenAppropriate(FakeOptions options, object result)
        {
            if (options.WrappedInstance != null)
            {
                this.wrapperConfigurator.ConfigureFakeToWrap(result, options.WrappedInstance, options.SelfInitializedFakeRecorder);
            }
        }

        private static void AssertThatProxyWasSuccessfullyCreated(Type typeOfProxy, ProxyResult proxyResult)
        {
            if (!proxyResult.ProxyWasSuccessfullyCreated)
            {
                var message = CreateFailedToGenerateProxyErrorMessage(typeOfProxy, proxyResult);
                throw new FakeCreationException(message);
            }
        }

        private static string CreateFailedToGenerateProxyErrorMessage(Type typeOfProxy, ProxyResult proxyResult)
        {
            var messageFromProxyGenerator = Indent(proxyResult.ErrorMessage);
            
            var writer = new StringWriter();

            writer.WriteLine();
            writer.WriteLine();
            writer.WriteLine(CommonExtensions.FormatInvariant(ExceptionMessages.FailedToGenerateProxyPattern, typeOfProxy, messageFromProxyGenerator));
            writer.WriteLine();

            return Indent(writer.GetStringBuilder().ToString());
        }

        private static string Indent(string value)
        { 
            var lines = value.Split(new [] {Environment.NewLine}, StringSplitOptions.None);

            return string.Join(Environment.NewLine,
                (from line in lines
                 select line.Length == 0 ? string.Empty : "  " + line).ToArray());

        }

        private FakeObject CreateNewFakeObject()
        {
            return this.fakeObjectFactory.Invoke();
        }
    }
}