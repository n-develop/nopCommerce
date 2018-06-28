using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Plugin.Pickup.PickupInStore.Data;
using Nop.Plugin.Pickup.PickupInStore.Domain;
using Nop.Plugin.Pickup.PickupInStore.Services;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Shipping.Pickup;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Pickup.PickupInStore
{
    public class PickupInStoreProvider : BasePlugin, IPickupPointProvider
    {
        #region Fields

        private readonly IAddressService _addressService;
        private readonly ICountryService _countryService;
        private readonly ILocalizationService _localizationService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreContext _storeContext;
        private readonly IStorePickupPointService _storePickupPointService;
        private readonly StorePickupPointObjectContext _objectContext;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public PickupInStoreProvider(IAddressService addressService,
            ICountryService countryService,
            ILocalizationService localizationService,
            IStateProvinceService stateProvinceService,
            IStoreContext storeContext,
            IStorePickupPointService storePickupPointService,
            StorePickupPointObjectContext objectContext, 
            IWebHelper webHelper)
        {
            this._addressService = addressService;
            this._countryService = countryService;
            this._localizationService = localizationService;
            this._stateProvinceService = stateProvinceService;
            this._storeContext = storeContext;
            this._storePickupPointService = storePickupPointService;
            this._objectContext = objectContext;
            this._webHelper = webHelper;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        public IShipmentTracker ShipmentTracker
        {
            get { return null; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get pickup points for the address
        /// </summary>
        /// <param name="address">Address</param>
        /// <returns>Represents a response of getting pickup points</returns>
        public GetPickupPointsResponse GetPickupPoints(Address address)
        {
            var result = new GetPickupPointsResponse();

            foreach (var point in _storePickupPointService.GetAllStorePickupPoints(_storeContext.CurrentStore.Id))
            {
                var pointAddress = _addressService.GetAddressById(point.AddressId);
                if (pointAddress == null)
                    continue;

                result.PickupPoints.Add(new PickupPoint
                {
                    Id = point.Id.ToString(),
                    Name = point.Name,
                    Description = point.Description,
                    Address = pointAddress.Address1,
                    City = pointAddress.City,
                    County = pointAddress.County,
                    StateAbbreviation = pointAddress.StateProvince?.Abbreviation ?? string.Empty,
                    CountryCode = pointAddress.Country?.TwoLetterIsoCode ?? string.Empty,
                    ZipPostalCode = pointAddress.ZipPostalCode,
                    OpeningHours = point.OpeningHours,
                    PickupFee = point.PickupFee,
                    DisplayOrder = point.DisplayOrder,
                    ProviderSystemName = PluginDescriptor.SystemName
                });
            }

            if (!result.PickupPoints.Any())
                result.AddError(_localizationService.GetResource("Plugins.Pickup.PickupInStore.NoPickupPoints"));

            return result;
        }

        /// <summary>
        /// Get pickup points for the address
        /// </summary>
        /// <param name="address">Address</param>
        /// <returns>Represents a response of getting pickup points</returns>
        public async Task<GetPickupPointsResponse> GetPickupPointsAsync(Address address)
        {
            var result = new GetPickupPointsResponse();

            //TODO Remove Task.Run(()=>{})
            return await Task.Run(() =>
            {
                foreach (var point in _storePickupPointService.GetAllStorePickupPoints(_storeContext.CurrentStore.Id))
                {
                    var pointAddress = _addressService.GetAddressById(point.AddressId);
                    if (pointAddress == null)
                        continue;

                    result.PickupPoints.Add(new PickupPoint
                    {
                        Id = point.Id.ToString(),
                        Name = point.Name,
                        Description = point.Description,
                        Address = pointAddress.Address1,
                        City = pointAddress.City,
                        County = pointAddress.County,
                        StateAbbreviation = pointAddress.StateProvince?.Abbreviation ?? string.Empty,
                        CountryCode = pointAddress.Country?.TwoLetterIsoCode ?? string.Empty,
                        ZipPostalCode = pointAddress.ZipPostalCode,
                        OpeningHours = point.OpeningHours,
                        PickupFee = point.PickupFee,
                        DisplayOrder = point.DisplayOrder,
                        ProviderSystemName = PluginDescriptor.SystemName
                    });
                }

                if (!result.PickupPoints.Any())
                    result.AddError(_localizationService.GetResource("Plugins.Pickup.PickupInStore.NoPickupPoints"));

                return result;
            });
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PickupInStore/Configure";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //database objects
            _objectContext.Install();

            //sample pickup point
            var country = _countryService.GetCountryByThreeLetterIsoCode("USA");
            var state = _stateProvinceService.GetStateProvinceByAbbreviation("NY", country?.Id);

            var address = new Address
            {
                Address1 = "21 West 52nd Street",
                City = "New York",
                CountryId = country?.Id,
                StateProvinceId = state?.Id,
                ZipPostalCode = "10021",
                CreatedOnUtc = DateTime.UtcNow
            };
            _addressService.InsertAddress(address);

            var pickupPoint = new StorePickupPoint
            {
                Name = "New York store",
                AddressId = address.Id,
                OpeningHours = "10.00 - 19.00",
                PickupFee = 1.99m
            };
            _storePickupPointService.InsertStorePickupPoint(pickupPoint);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Pickup.PickupInStore.AddNew", "Add a new pickup point");
            this.AddOrUpdatePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.Description", "Description");
            this.AddOrUpdatePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.Description.Hint", "Specify a description of the pickup point.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.DisplayOrder", "Display order");
            this.AddOrUpdatePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.DisplayOrder.Hint", "Specify the pickup point display order.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.Name", "Name");
            this.AddOrUpdatePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.Name.Hint", "Specify a name of the pickup point.");            
            this.AddOrUpdatePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.OpeningHours", "Opening hours");
            this.AddOrUpdatePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.OpeningHours.Hint", "Specify an openning hours of the pickup point (Monday - Friday: 09:00 - 19:00 for example).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.PickupFee", "Pickup fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.PickupFee.Hint", "Specify a fee for the shipping to the pickup point.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.Store", "Store");
            this.AddOrUpdatePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.Store.Hint", "A store name for which this pickup point will be available.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Pickup.PickupInStore.NoPickupPoints", "No pickup points are available");

            base.Install();
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override async Task InstallAsync()
        {
            //TODO Remove Task.Run(()=>{})
            await Task.Run(() =>
            {
                //database objects
                _objectContext.Install();

                //sample pickup point
                var country = _countryService.GetCountryByThreeLetterIsoCode("USA");
                var state = _stateProvinceService.GetStateProvinceByAbbreviation("NY", country?.Id);

                var address = new Address
                {
                    Address1 = "21 West 52nd Street",
                    City = "New York",
                    CountryId = country?.Id,
                    StateProvinceId = state?.Id,
                    ZipPostalCode = "10021",
                    CreatedOnUtc = DateTime.UtcNow
                };
                _addressService.InsertAddress(address);

                var pickupPoint = new StorePickupPoint
                {
                    Name = "New York store",
                    AddressId = address.Id,
                    OpeningHours = "10.00 - 19.00",
                    PickupFee = 1.99m
                };
                _storePickupPointService.InsertStorePickupPoint(pickupPoint);
            });

            //locales
            await this.AddOrUpdatePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.AddNew", "Add a new pickup point");
            await this.AddOrUpdatePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.Description", "Description");
            await this.AddOrUpdatePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.Description.Hint", "Specify a description of the pickup point.");
            await this.AddOrUpdatePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.DisplayOrder", "Display order");
            await this.AddOrUpdatePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.DisplayOrder.Hint", "Specify the pickup point display order.");
            await this.AddOrUpdatePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.Name", "Name");
            await this.AddOrUpdatePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.Name.Hint", "Specify a name of the pickup point.");            
            await this.AddOrUpdatePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.OpeningHours", "Opening hours");
            await this.AddOrUpdatePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.OpeningHours.Hint", "Specify an openning hours of the pickup point (Monday - Friday: 09:00 - 19:00 for example).");
            await this.AddOrUpdatePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.PickupFee", "Pickup fee");
            await this.AddOrUpdatePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.PickupFee.Hint", "Specify a fee for the shipping to the pickup point.");
            await this.AddOrUpdatePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.Store", "Store");
            await this.AddOrUpdatePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.Store.Hint", "A store name for which this pickup point will be available.");
            await this.AddOrUpdatePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.NoPickupPoints", "No pickup points are available");
             
            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //database objects
            _objectContext.Uninstall();

            //locales
            this.DeletePluginLocaleResource("Plugins.Pickup.PickupInStore.AddNew");
            this.DeletePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.Description");
            this.DeletePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.Description.Hint");
            this.DeletePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.DisplayOrder");
            this.DeletePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.DisplayOrder.Hint");
            this.DeletePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.Name");
            this.DeletePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.Name.Hint");
            this.DeletePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.OpeningHours");
            this.DeletePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.OpeningHours.Hint");
            this.DeletePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.PickupFee");
            this.DeletePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.PickupFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.Store");
            this.DeletePluginLocaleResource("Plugins.Pickup.PickupInStore.Fields.Store.Hint");
            this.DeletePluginLocaleResource("Plugins.Pickup.PickupInStore.NoPickupPoints");

            base.Uninstall();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override async Task UninstallAsync()
        {
            //TODO Remove Task.Run(()=>{})
            await Task.Run(() =>
            {
                //database objects
                _objectContext.Uninstall();
            });

            //locales
            await this.DeletePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.AddNew");
            await this.DeletePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.Description");
            await this.DeletePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.Description.Hint");
            await this.DeletePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.DisplayOrder");
            await this.DeletePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.DisplayOrder.Hint");
            await this.DeletePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.Name");
            await this.DeletePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.Name.Hint");
            await this.DeletePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.OpeningHours");
            await this.DeletePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.OpeningHours.Hint");
            await this.DeletePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.PickupFee");
            await this.DeletePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.PickupFee.Hint");
            await this.DeletePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.Store");
            await this.DeletePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.Fields.Store.Hint");
            await this.DeletePluginLocaleResourceAsync("Plugins.Pickup.PickupInStore.NoPickupPoints");

            await base.UninstallAsync();
        }

        #endregion
    }
}
