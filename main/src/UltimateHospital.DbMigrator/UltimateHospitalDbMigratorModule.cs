using UltimateHospital.RuntimeInfrastructure;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace UltimateHospital.DbMigrator;

[DependsOn(typeof(AbpAutofacModule), typeof(RuntimeInfrastructureModule))]
public sealed class UltimateHospitalDbMigratorModule : AbpModule;
