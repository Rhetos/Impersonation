# ComplexEntity

ComplexEntity is a DSL package (a plugin module) for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It provides functionality for manipulation with complex entities (such as master-detail) in same transaction. In order to work properly it introduces sevareal concepts needed to implement this functionality - Function, Object and ListOf.

Contents:

1. [ComplexEntity](#ComplexEntity)
2. [Function](#Function)
3. [Object and ListOf](#Object-and-ListOf)

See [rhetos.org](http://www.rhetos.org/) for more information on Rhetos.

## ComplexEntity

ComplexEntity is DSL concept that load and save complex data in a single request. That data can consist of several entities that have some kind of relation on to the other. Relationships that can be used are Detail and Extends. Defining a ComplexEntity requires a root entity, which is navigational anchor of the ComplexEntity. Dependency order is calculated from that entity. 

Example:

```c
Module ComplexEntities
{
    Entity MasterEntity
    {
        ShortString Title;
    }

    Entity ExtensionEntity
    {
        Extends ComplexEntities.MasterEntity;
        ShortString Description;
    }

    Entity DetailEntity
    {
        Reference MasterEntity { Detail; }
        Integer OrderNumber;
    }

    ComplexEntity MasterDetailEntity 'ComplexEntities.MasterEntity'
    {
        ListOf ComplexEntities.DetailEntity Details;
        Object ComplexEntities.ExtensionEntity Extension;
        Bool PerformPostValidation;
        
        BeforeSave ValidateData
        '
            if (parameters.Item.Details == null || !parameters.Item.Details.Any())
                throw new Rhetos.UserException("Entity must have at least one detail.");
        ';

        AfterSave SomeAfterSaveValidation
        '
            if (parameters.PerformPostValidation.GetValueOrDefault())
            {
                // some after save optional validation
                // throw new Rhetos.UserException("Some validation description."); - this exception rollbacks entire transaction, nothing is saved
            }
        ';
    }
}
```

ComplexEntity concept creates functions for saving and loading defiend data, prefixed with Save and Get. For ComplexEntity defined in the example above they would be SaveMasterDetailEntity and GetMasterDetailEntity. SaveMasterDetailEntity accepts parameter Item of type MasterDetailEntity, and GetMasterDetailEntity accepts parametar ID of type Guid which represents id of root entity of ComplexEntity which we want to load. Definded ComplexEntity has all the properties that root entity has.

Additionaly, concepts BeforeSave and AfterSave are provided that allows you to insert your code before and after save sequence for preprocess and postprocess purposes.

### Using ComplexEntity in Domain Object Model

After reading [How to execute examples](https://github.com/Rhetos/Rhetos/wiki/Using-the-Domain-Object-Model#how-to-execute-the-examples) you can try this example of saving and loading with ComplexEntity that uses DSL described above:

```cs
void Main()
{
    ConsoleLogger.MinLevel = EventType.Info; // Use "Trace" for more detailed log.
    var rhetosServerPath = Path.GetDirectoryName(Util.CurrentQueryPath);
    Directory.SetCurrentDirectory(rhetosServerPath);
    // Set commitChanges parameter to COMMIT or ROLLBACK the data changes.
    using (var container = new RhetosTestContainer(commitChanges: false))
    {
        var context = container.Resolve<Common.ExecutionContext>();

        var id = Guid.NewGuid();
        var complex = new ComplexEntities.MasterDetailEntity
        {
            ID = id,
            Title = "My first complex entity",
            Details = new List<ComplexEntities.DetailEntity>
            {
                new ComplexEntities.DetailEntity {ID = Guid.NewGuid(), MasterEntityID = id, Description = "Detail a"},
                new ComplexEntities.DetailEntity {ID = Guid.NewGuid(), MasterEntityID = id, Description = "Detail b"}
            },
            Extension = new ComplexEntities.ExtensionEntity { OrderNumber = 123},
            PerformPostValidation = false
        };
		
		//save complex entity
        context.Repository.ComplexEntities.SaveMasterDetailEntity.Execute(new ComplexEntities.SaveMasterDetailEntity { Item = complex });
        
		//load complex entity
        var loaded = context.Repository.ComplexEntities.GetMasterDetailEntity.Execute(new ComplexEntities.GetMasterDetailEntity { ID = id });
    }
}
```

## Function

Function is simmilar to [Action concept](https://github.com/Rhetos/Rhetos/wiki/Action-concept), except it allows you to return some result. Additional parameter specifies type of return value. For example:

```c
Module Functions
{
    Entity Post
    {
      ShortString Title;
      LongString Content;
    }

    Function CreateNewPost Functions.Post //returns instance of type Functions.Post
    '(parameter, repository, userInfo) =>
    {
        var content = parameter.Content ?? "Default Content";

        var post = new Functions.Post 
        {
          Title = parameter.Title,
          Content = content,
        };

        repository.Functions.Post.Insert(post);

        return post;
    }'
    {
        ShortString Title;
        LongString Content;
    }
}
```

## Object and ListOf

Object is a DSL concept which allows you to define a DataStructure property that is a reference type of another DataStructure. 

ListOf is DSL concept which allows you to define a DataStructure property that is a list of type of another DataStructure or simple property (ShortString, Integer, etc.). In C# Domain Object Model it is represented as `List<T>`.

Example:

```c
Module Functions
{
    Entity Post
    {
        ShortString Title;
        LongString Content;
    }
	
	Parameter ReturnValue
	{
		ShortString SomeValue;
		Object Functions.Post Post;
		ListOf Functions.Post AdditionalPosts;
		ListOf ShortString Contributors;
	}
}
```
