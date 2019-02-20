### Technical support web-api example
#### ASP Net Core 2.2, ASP MVC, REST
###
#### Highlights: 
- REST, task-based pattern;
- message queue as database table;
- DBContext DI;
- configurable test console app with support user and employee simulators;
- 3 types of employees: operator - processes all messages, manager - processes messages of age > (Tm), director - processes messages of age > (Td), minimum (Tmin) and maximun (Tmax) interval for employee simulator to process message and aquire next one, minimum (T) and maximun (Tc) interval for user simulator to use service;
