using AutoMapper;
using VR.Mapping;

public static class MyMapper
{
    public static IMapper Mapper { get; private set; }

    private static bool _isInitialized;
    public static void Initialize()
    {
        if (!_isInitialized)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<VocabularyMappingProfile>();
            });

            Mapper = config.CreateMapper();
            _isInitialized = true;
        }
    }
}