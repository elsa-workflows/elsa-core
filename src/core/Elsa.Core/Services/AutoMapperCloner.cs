using AutoMapper;

namespace Elsa.Services
{
    /// <summary>
    /// An implementation that relies on AutoMapper to clone objects. This requires the types that need to be supported to be registered with AutoMapper.
    /// We might eventually consider doing a different implementation that does not require explicitly registering which types support cloning.
    /// </summary>
    public class AutoMapperCloner : ICloner
    {
        private readonly IMapper _mapper;
        public AutoMapperCloner(IMapper mapper) => _mapper = mapper;
        public T Clone<T>(T source) => _mapper.Map<T>(source);
    }
}