using UltimateHospital.RuntimeInfrastructure;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace UltimateHospital.HttpApi.Host;

[DependsOn(typeof(AbpAspNetCoreMvcModule), typeof(AbpAutofacModule), typeof(RuntimeInfrastructureModule))]
public sealed class UltimateHospitalHttpApiHostModule : AbpModule;
