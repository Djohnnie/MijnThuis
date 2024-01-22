using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MijnThuis.Contracts.Car;

public class GetCarOverviewQuery : IRequest<GetCarOverviewResponse>
{
}