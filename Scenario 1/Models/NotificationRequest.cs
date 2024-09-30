using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scenario_1.Models
{
    public record NotificationRequest(string Recipient, string Type, string Message);
}
