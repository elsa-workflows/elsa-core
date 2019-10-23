# Creating and updating the Sqlite database

To create the database, run the following command from a shell:

 ```bash
dotnet ef database update
```

> NOTE: By default, the SqLite provider is used, and will create a database called `elsa.db` in folder `c:\data`. You will have to create this folder manually if it doesn't exist. 