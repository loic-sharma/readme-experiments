## Library Powered By

This library is powered by [Entity Framework Extensions](https://entityframework-extensions.net/?z=github&y=entityframework-plus)

<a href="https://entityframework-extensions.net/?z=github&y=entityframework-plus">
<kbd>
<img src="https://zzzprojects.github.io/images/logo/entityframework-extensions-pub.jpg" alt="Entity Framework Extensions" />
</kbd>
</a>

---

**IMPORTANT:** This library is no longer supported since 2015. 

We highly recommend you to move to:
- [Entity Framework Extensions](https://entityframework-extensions.net/?z=ef-extended) for pro features
- [Entity Framework Plus](https://entityframework-plus.net/?z=ef-extended) for free features

### Entity Framework Extensions
Website: [https://entityframework-extensions.net/](https://entityframework-extensions.net/?z=ef-extended)

Paid library to dramatically improve Entity Framework performance:

- BulkSaveChanges
- BulkInsert
- BulkUpdate
- BulkDelete
- BulkMerge
- BulkSynchronize

### Entity Framework Plus
Website: [https://entityframework-plus.net/](https://entityframework-plus.net/?z=ef-extended)

Free & Open source library that support following features:

- Audit
- Batch Operations
    - Batch Delete
    - Batch Update
- Query
    - Query Cache 
    - Query Deferred
    - Query Filter
    - Query Future
    - Query IncludeFilter
    - Query IncludeOptimized

# What's Entity Framework Extended? 

## Download

The Entity Framework Extended library is available on nuget.org via package name `EntityFramework.Extended`.

To install EntityFramework.Extended, run the following command in the Package Manager Console.

    PM> Install-Package EntityFramework.Extended
    
## Features

- [Batch Update and Delete](https://github.com/loresoft/EntityFramework.Extended/wiki/Batch-Update-and-Delete)
- [Future Queries](https://github.com/loresoft/EntityFramework.Extended/wiki/Future-Queries)
- [Query Result Cache](https://github.com/loresoft/EntityFramework.Extended/wiki/Query-Result-Cache)
- [Audit Log](https://github.com/loresoft/EntityFramework.Extended/wiki/Audit-Log)
 
### Batch Update and Delete

The Entity Framework's current limitation is that you have first to retrieve it into memory to update or delete an entity. Now in most scenarios, this is just fine. There are, however, some scenarios where performance would suffer. Also, the object must be retrieved for single deletes before it can be deleted, requiring two calls to the database. Batch update and delete eliminate the need to retrieve and load an entity before modifying it.

**Deleting**
    
    //delete all users where FirstName matches
    context.Users
        .Where(u => u.FirstName == "firstname")
        .Delete();

**Update**
    
    //update all tasks with status of 1 to status of 2
    context.Tasks
        .Where(t => t.StatusId == 1)
        .Update(t => new Task { StatusId = 2 });
    
    //example of using an IQueryable as the filter for the update
    var users = context.Users.Where(u => u.FirstName == "firstname");
    context.Users.Update(users, u => new User {FirstName = "newfirstname"});

### Future Queries

Build up a list of queries for the data that you need, and the first time any of the results are accessed, all the data will retrieved in one round trip to the database server. Reducing the number of trips to the database is a great. Using this feature is as simple as appending `.Future()` to the end of your queries to use the Future Queries. 

Future queries are created with the following extension methods:

- Future()
- FutureFirstOrDefault()
- FutureCount()

Sample

    // build up queries
    var q1 = db.Users
        .Where(t => t.EmailAddress == "one@test.com")
        .Future();
    
    var q2 = db.Tasks
        .Where(t => t.Summary == "Test")
        .Future();
    
    // this triggers the loading of all the future queries
    var users = q1.ToList();

In the example above, there are two queries built up. As soon as one of the queries is enumerated, it triggers the batch load of both queries.
     
    // base query
    var q = db.Tasks.Where(t => t.Priority == 2);
    // get total count
    var q1 = q.FutureCount();
    // get page
    var q2 = q.Skip(pageIndex).Take(pageSize).Future();
    
    // triggers execute as a batch
    int total = q1.Value;
    var tasks = q2.ToList();
    
In this example, we have a common scenario where you want to page a list of tasks. For the GUI to set up the paging control, you need a total count. With Future, we can batch together the queries to get all the data in one database call.

Future queries work by creating the appropriate IFutureQuery object that keeps the IQuerable. The IFutureQuery object is then stored in IFutureContext.FutureQueries list. Then, when one of the IFutureQuery objects is enumerated, it calls back to IFutureContext.ExecuteFutureQueries() via the LoadAction delegate. ExecuteFutureQueries builds a batch query from all the stored IFutureQuery objects. Finally, all the IFutureQuery objects are updated with the results from the query.

### Query Result Cache

To cache query results, use the `FromCache` extension method. Below is a caching query result. Construct the LINQ query as you normally would, then append the `FromCache` extension.
     
    //query is cached using the default settings
    var tasks = db.Tasks
        .Where(t => t.CompleteDate == null)
        .FromCache();
 
    //query result is now cached 300 seconds
    var tasks = db.Tasks
        .Where(t => t.AssignedId == myUserId && t.CompleteDate == null)
        .FromCache(CachePolicy.WithDurationExpiration(TimeSpan.FromSeconds(300)));
        
The Query Result Cache also supports tagging the cache so you can expire common cache entries by calling `Expire` on a cache tag.

    // cache assigned tasks
    var tasks = db.Tasks
        .Where(t => t.AssignedId == myUserId && t.CompleteDate == null)
        .FromCache(tags: new[] { "Task", "Assigned-Task-" + myUserId  });

    // some update happened to Task, expire Task tag
    CacheManager.Current.Expire("Task");
    
The `CacheManager` has support for providers.  The default provider uses `MemoryCache` to store the cache entries.  To create a custom provider, implement `ICacheProvider`. The custom provider will then need to be registered in the `Locator` container.

    // Replace cache provider with Memcached provider
    Locator.Current.Register<ICacheProvider>(() => new MemcachedProvider());

### Audit Log

The Audit Log feature will capture the changes to entities anytime they are submitted to the database. The Audit Log captures only the changed entities and only the changed properties. The before and after values are recorded. `AuditLogger.LastAudit` is where this information is held and there is a `ToXml()` method that makes it easy to turn the AuditLog into xml for easy storage.

The AuditLog can be customized via attributes on the entities or via a Fluent Configuration API.

Fluent Configuration
    
    // config audit when your application is starting up...
    var auditConfiguration = AuditConfiguration.Default;
    
    auditConfiguration.IncludeRelationships = true;
    auditConfiguration.LoadRelationships = true;
    auditConfiguration.DefaultAuditable = true;
    
    // customize the audit for Task entity
    auditConfiguration.IsAuditable<Task>()
        .NotAudited(t => t.TaskExtended)
        .FormatWith(t => t.Status, v => FormatStatus(v));
    
    // set the display member when status is a foreign key
    auditConfiguration.IsAuditable<Status>()
        .DisplayMember(t => t.Name);

Create an Audit Log

    var db = new TrackerContext();
    var audit = db.BeginAudit();

    // make some updates ...

    db.SaveChanges();
    var log = audit.LastLog;

## Useful links

- [Documentation](https://entityframework.net/ef-extended)
- [NuGet](https://nuget.org/packages/EntityFramework.Extended)
- You can also consult several questions on 
[Stack Overflow](https://stackoverflow.com/questions/tagged/entity-framework-extended)

## Contribute

Want to help us? Your donation directly helps us maintain and grow ZZZ Free Projects. 

We can't thank you enough for your support 🙏.

👍 [One-time donation](https://zzzprojects.com/contribute)

❤️ [Become a sponsor](https://github.com/sponsors/zzzprojects) 

### Why should I contribute to this free & open-source library?
We all love free and open-source libraries! But there is a catch... nothing is free in this world.

We NEED your help. Last year alone, we spent over **3000 hours** maintaining all our open source libraries.

Contributions allow us to spend more of our time on: Bug Fix, Development, Documentation, and Support.

### How much should I contribute?
Any amount is much appreciated. All our free libraries together have more than **100 million** downloads.

If everyone could contribute a tiny amount, it would help us make the .NET community a better place to code!

Another great free way to contribute is  **spreading the word** about the library.

A **HUGE THANKS** for your help!

## More Projects

- [EntityFramework Extensions](https://entityframework-extensions.net/)
- [Dapper Plus](https://dapper-plus.net/)
- [C# Eval Expression](https://eval-expression.net/)
- and much more! 

To view all our free and paid projects, visit our [website](https://zzzprojects.com/).
