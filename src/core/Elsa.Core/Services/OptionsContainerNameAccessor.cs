using Elsa.Options;

namespace Elsa.Services
{
    public class OptionsContainerNameAccessor : IContainerNameAccessor
    {
        private readonly ElsaOptions _elsaOptions;
        public OptionsContainerNameAccessor(ElsaOptions elsaOptions) => _elsaOptions = elsaOptions;
        public string GetContainerName() => _elsaOptions.ContainerName;
    }
}