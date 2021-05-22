using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyLogs
{
    public class Person
    {
        public string Name { get; set; }
        private readonly ILogger<Person> _logger;

        public Person(ILogger<Person> logger)
        {
            _logger = logger;
        }

        public void Talk(string text)
        {
            _logger.LogInformation("Person {name} spoke {text}", Name, text);
        }
    }
}
