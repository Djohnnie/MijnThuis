using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Car;
using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Entities;

namespace MijnThuis.Application.Car.Commands;

public class SetManualCarChargeCommandHandler : IRequestHandler<SetManualCarChargeCommand, SetManualCarChargeResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public SetManualCarChargeCommandHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SetManualCarChargeResponse> Handle(SetManualCarChargeCommand request, CancellationToken cancellationToken)
    {
        var flag = await _dbContext.Flags.SingleOrDefaultAsync(x => x.Name == ManualCarChargeFlag.Name, cancellationToken);

        var value = new ManualCarChargeFlag
        {
            ShouldCharge = request.IsEnabled,
            ChargeAmps = request.ChargeAmps
        }.Serialize();

        if (flag == null)
        {
            _dbContext.Flags.Add(new Flag
            {
                Id = Guid.NewGuid(),
                Name = ManualCarChargeFlag.Name,
                Value = value
            });
        }
        else
        {
            flag.Value = value;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SetManualCarChargeResponse();
    }
}