using Sieve.Services;
using Sieve.Models;
using Microsoft.Extensions.Options;
using MyBoards.Entities;

namespace MyBoards.Sieve
{
    public class ApplicationSieveProcessor : SieveProcessor
    {
        public ApplicationSieveProcessor(IOptions<SieveOptions> options) : base(options) 
        {
        }

        protected override SievePropertyMapper MapProperties(SievePropertyMapper mapper)
        {
            mapper.Property<Epic>(e => e.Priority)
                .CanSort()
                .CanFilter();

            mapper.Property<Epic>(e => e.Area)
                .CanSort()
                .CanFilter();

            mapper.Property<Epic>(e => e.StartDate)
               .CanSort()
               .CanFilter();

            mapper.Property<Epic>(e => e.Author.FullName)
               .CanSort()
               .CanFilter();

            return mapper;
        }

    }
}
