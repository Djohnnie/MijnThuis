using MediatR;
using Microsoft.Extensions.Configuration;
using MijnThuis.Contracts.Car;
using MijnThuis.Integrations.Car;

namespace MijnThuis.Application.Car.Commands;

public class CarOverheatProtectionCommandHandler : IRequestHandler<CarOverheatProtectionCommand, CarCommandResponse>
{
    private readonly ICarService _carService;
    private readonly IConfiguration _configuration;

    public CarOverheatProtectionCommandHandler(
        ICarService carService,
        IConfiguration configuration)
    {
        _carService = carService;
        _configuration = configuration;
    }

    public async Task<CarCommandResponse> Handle(CarOverheatProtectionCommand request, CancellationToken cancellationToken)
    {
        var pin = _configuration.GetValue<string>("PINCODE");

        if (request.Pin != pin)
        {
            return new CarCommandResponse
            {
                Success = false
            };
        }

        var result = await _carService.SetCabinHeatProtection(request.Enable, fanOnly: false);

        return new CarCommandResponse
        {
            Success = result
        };
    }
}