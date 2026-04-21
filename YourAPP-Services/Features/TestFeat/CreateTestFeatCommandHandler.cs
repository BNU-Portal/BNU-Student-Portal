using MediatR;
using YourAPP_Domain.Entities;
using YourAPP_Domain.Interfaces;

namespace YourAPP_Services.Features.TestFeat
{
    public class CreateTestFeatCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateTestFeatCommand, int>
    {


        public async Task<int> Handle(CreateTestFeatCommand request, CancellationToken cancellationToken)
        {
            var repo = unitOfWork.GetRepository<TestFeatEntity, int>();


            var entity = new TestFeatEntity
            {
                Id = request.id,
                Name = request.name
            };

            await repo.AddAsync(entity);
            await unitOfWork.SaveChangesAsync();
            return entity.Id;

        }
    }
}
