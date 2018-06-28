using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Core.Infrastructure.Mapper;
using Nop.Core.Plugins;

namespace Nop.Core.Infrastructure
{
    /// <summary>
    /// Represents Nop engine
    /// </summary>
    public partial class NopEngine : IEngine
    {
        #region Fields

        private IServiceProvider _serviceProvider;

        #endregion

        #region Utilities

        /// <summary>
        /// Get service provider
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains service provider</returns>
        protected virtual async Task<IServiceProvider> GetServiceProviderAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var accessor = ServiceProvider.GetService<IHttpContextAccessor>();
                return accessor.HttpContext?.RequestServices ?? ServiceProvider;
            }, cancellationToken);
        }

        /// <summary>
        /// Run startup tasks
        /// </summary>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that startup tasks are executed</returns>
        protected virtual async Task RunStartupTasksAsync(ITypeFinder typeFinder, CancellationToken cancellationToken)
        {
            //find startup tasks provided by other assemblies
            var startupTasks = await typeFinder.FindClassesOfTypeAsync<IStartupTask>(cancellationToken: cancellationToken);

            //create and sort instances of startup tasks
            var instances = startupTasks
                .Where(startupTask => PluginManager.FindPlugin(startupTask)?.Installed ?? true) //ignore not installed plugins
                .Select(startupTask => (IStartupTask)Activator.CreateInstance(startupTask))
                .OrderBy(startupTask => startupTask.Order);

            //execute tasks
            foreach (var task in instances)
                await task.ExecuteAsync(cancellationToken);
        }

        /// <summary>
        /// Register dependencies using Autofac
        /// </summary>
        /// <param name="nopConfig">Startup Nop configuration parameters</param>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains service provider</returns>
        protected virtual async Task<IServiceProvider> RegisterDependenciesAsync(NopConfig nopConfig, IServiceCollection services,
            ITypeFinder typeFinder, CancellationToken cancellationToken)
        {
            var containerBuilder = new ContainerBuilder();

            //register engine
            containerBuilder.RegisterInstance(this).As<IEngine>().SingleInstance();

            //register type finder
            containerBuilder.RegisterInstance(typeFinder).As<ITypeFinder>().SingleInstance();

            //find dependency registrars provided by other assemblies
            var dependencyRegistrars = await typeFinder.FindClassesOfTypeAsync<IDependencyRegistrar>(cancellationToken: cancellationToken);

            //create and sort instances of dependency registrars
            var instances = dependencyRegistrars
                //.Where(dependencyRegistrar => PluginManager.FindPlugin(dependencyRegistrar)?.Installed ?? true) //ignore not installed plugins
                .Select(dependencyRegistrar => (IDependencyRegistrar)Activator.CreateInstance(dependencyRegistrar))
                .OrderBy(dependencyRegistrar => dependencyRegistrar.Order);

            //register all provided dependencies
            foreach (var dependencyRegistrar in instances)
                await dependencyRegistrar.RegisterAsync(containerBuilder, typeFinder, nopConfig, cancellationToken);

            //populate Autofac container builder with the set of registered service descriptors
            containerBuilder.Populate(services);

            //create service provider
            _serviceProvider = new AutofacServiceProvider(containerBuilder.Build());
            return _serviceProvider;
        }

        /// <summary>
        /// Register and configure AutoMapper
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that AutoMapper is configured</returns>
        protected virtual async Task AddAutoMapperAsync(IServiceCollection services, ITypeFinder typeFinder, CancellationToken cancellationToken)
        {
            //find mapper configurations provided by other assemblies
            var mapperConfigurations = await typeFinder.FindClassesOfTypeAsync<IOrderedMapperProfile>(cancellationToken: cancellationToken);

            //create and sort instances of mapper configurations
            var instances = mapperConfigurations
                .Where(mapperConfiguration => PluginManager.FindPlugin(mapperConfiguration)?.Installed ?? true) //ignore not installed plugins
                .Select(mapperConfiguration => (IOrderedMapperProfile)Activator.CreateInstance(mapperConfiguration))
                .OrderBy(mapperConfiguration => mapperConfiguration.Order);

            //create AutoMapper configuration
            var config = new MapperConfiguration(cfg =>
            {
                foreach (var instance in instances)
                {
                    cfg.AddProfile(instance.GetType());
                }
            });

            //register AutoMapper
            services.AddAutoMapper();

            //register
            AutoMapperConfiguration.Init(config);
        }

        protected virtual Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //check for assembly already loaded
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            //get assembly from TypeFinder
            var tf = ResolveAsync<ITypeFinder>(default(CancellationToken)).Result;
            assembly = tf.GetAssembliesAsync(default(CancellationToken)).Result.FirstOrDefault(a => a.FullName == args.Name);
            return assembly;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize engine
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that engine is initialized</returns>
        public virtual async Task InitializeAsync(IServiceCollection services, CancellationToken cancellationToken)
        {
            //most of API providers require TLS 1.2 nowadays
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var provider = services.BuildServiceProvider();
            var hostingEnvironment = provider.GetRequiredService<IHostingEnvironment>();
            CommonHelper.DefaultFileProvider = new NopFileProvider(hostingEnvironment);

            //initialize plugins
            var nopConfig = provider.GetRequiredService<NopConfig>();
            var mvcCoreBuilder = services.AddMvcCore();
            await PluginManager.InitializeAsync(mvcCoreBuilder.PartManager, nopConfig, cancellationToken);
        }

        /// <summary>
        /// Add and configure services
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains service provider</returns>
        public virtual async Task<IServiceProvider> ConfigureServicesAsync(IServiceCollection services, IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            //find startup configurations provided by other assemblies
            var typeFinder = new WebAppTypeFinder();
            var startupConfigurations = await typeFinder.FindClassesOfTypeAsync<INopStartup>(cancellationToken: cancellationToken);

            //create and sort instances of startup configurations
            var instances = startupConfigurations
                //.Where(startup => PluginManager.FindPlugin(startup)?.Installed ?? true) //ignore not installed plugins
                .Select(startup => (INopStartup)Activator.CreateInstance(startup))
                .OrderBy(startup => startup.Order);

            //configure services
            foreach (var instance in instances)
                await instance.ConfigureServicesAsync(services, configuration, cancellationToken);

            //register mapper configurations
            await AddAutoMapperAsync(services, typeFinder, cancellationToken);

            //register dependencies
            var nopConfig = services.BuildServiceProvider().GetService<NopConfig>();
            await RegisterDependenciesAsync(nopConfig, services, typeFinder, cancellationToken);

            //run startup tasks
            if (!nopConfig.IgnoreStartupTasks)
                await RunStartupTasksAsync(typeFinder, cancellationToken);

            //resolve assemblies here. otherwise, plugins can throw an exception when rendering views
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            return _serviceProvider;
        }

        /// <summary>
        /// Configure HTTP request pipeline
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that request pipeline is configured</returns>
        public virtual async Task ConfigureRequestPipelineAsync(IApplicationBuilder application, CancellationToken cancellationToken)
        {
            //find startup configurations provided by other assemblies
            var typeFinder = await ResolveAsync<ITypeFinder>(cancellationToken);
            var startupConfigurations = await typeFinder.FindClassesOfTypeAsync<INopStartup>(cancellationToken: cancellationToken);

            //create and sort instances of startup configurations
            var instances = startupConfigurations
                //.Where(startup => PluginManager.FindPlugin(startup)?.Installed ?? true) //ignore not installed plugins
                .Select(startup => (INopStartup)Activator.CreateInstance(startup))
                .OrderBy(startup => startup.Order);

            //configure request pipeline
            foreach (var instance in instances)
                await instance.ConfigureAsync(application, cancellationToken);
        }

        /// <summary>
        /// Resolve dependency
        /// </summary>
        /// <typeparam name="T">Type of resolved service</typeparam>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains resolved service</returns>
        public virtual async Task<T> ResolveAsync<T>(CancellationToken cancellationToken) where T : class
        {
            var serviceProvider = await GetServiceProviderAsync(cancellationToken);
            return (T)serviceProvider.GetRequiredService(typeof(T));
        }

        /// <summary>
        /// Resolve dependency
        /// </summary>
        /// <param name="type">Type of resolved service</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains resolved service</returns>
        public virtual async Task<object> ResolveAsync(Type type, CancellationToken cancellationToken)
        {
            var serviceProvider = await GetServiceProviderAsync(cancellationToken);
            return serviceProvider.GetRequiredService(type);
        }

        /// <summary>
        /// Resolve dependencies
        /// </summary>
        /// <typeparam name="T">Type of resolved services</typeparam>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains collection of resolved services</returns>
        public virtual async Task<IEnumerable<T>> ResolveAllAsync<T>(CancellationToken cancellationToken)
        {
            var serviceProvider = await GetServiceProviderAsync(cancellationToken);
            return (IEnumerable<T>)serviceProvider.GetServices(typeof(T));
        }

        /// <summary>
        /// Resolve unregistered service
        /// </summary>
        /// <param name="type">Type of service</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains resolved service</returns>
        public virtual async Task<object> ResolveUnregisteredAsync(Type type, CancellationToken cancellationToken)
        {
            Exception innerException = null;
            foreach (var constructor in type.GetConstructors())
            {
                try
                {
                    //try to resolve constructor parameters
                    var parameters = await Task.WhenAll(constructor.GetParameters().Select(async parameter =>
                    {
                        return await ResolveAsync(parameter.ParameterType, cancellationToken)
                            ?? throw new NopException("Unknown dependency");
                    }));

                    //all is ok, so create instance
                    return Activator.CreateInstance(type, parameters.ToArray());
                }
                catch (Exception ex)
                {
                    innerException = ex;
                }
            }

            throw new NopException("No constructor was found that had all the dependencies satisfied.", innerException);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the service provider
        /// </summary>
        public virtual IServiceProvider ServiceProvider => _serviceProvider;

        #endregion
    }
}