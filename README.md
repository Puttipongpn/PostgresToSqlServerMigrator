# SYNC-OCC-SQLSERVER-MAIN

A .NET Core console application to **sync data from PostgreSQL to SQL Server**. Designed for batch processing, optional table creation, and configurable table lists for both ERM and OCC systems.

Project Status: Under Development
This project is currently in the development phase and is intended for educational purposes only. 
Please note that the code has not yet been optimized or refined for general usability.

---

## ✨ Features

- **High Performance**: Sync data in batches for optimized performance.
- **Schema Flexibility**: Auto-create SQL Server tables based on PostgreSQL schema (optional).
- **Interactive Console**: Choose databases and options interactively.
- **Customizable**: Configure source/destination connections and table lists via JSON files.
- **Multi-Table Support**: Sync multiple tables in a single run.

---

## 📁 Configuration

### `appsettings.example.json`

Below is an example configuration file for database connections:

```json
{
    "ConnectionStrings": {
        "SourceConnectionStringERM": "Host=xxx;Username=xxx;Password=xxx;Database=db;Port=5432;",
        "SourceConnectionStringOCC": "Host=xxx;Username=xxx;Password=xxx;Database=db;Port=5432;",
        "DestConnectionStringERM": "Data Source=xxx;Initial Catalog=db;User Id=xxx;Password=xxx;",
        "DestConnectionStringOCC": "Data Source=xxx;Initial Catalog=db;User Id=xxx;Password=xxx;"
    }
}
```

---

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/sync-occ-sqlserver.git
cd sync-occ-sqlserver
```

### 2. Configure Connections

Edit the `appsettings.json` file (not committed to the repository). Use the following structure as a reference:

```json
{
    "ConnectionStrings": {
        "SourceConnectionStringERM": "Host=xxx.xx.xx.xx;Username=xxxxx;Password=xxxxx;Database=dbname;Port=xxxx;",
        "SourceConnectionStringOCC": "Host=xxx.xx.xx.xx;Username=xxxxx;Password=xxxxx;Database=dbname;Port=xxxx;",
        "DestConnectionStringERM": "Data Source=xxx.xx.xx.xx;Initial Catalog=dbname;User id=xxxx;Password=xxxx;",
        "DestConnectionStringOCC": "Data Source=xxx.xx.xx.xx;Initial Catalog=dbname;User id=xxxx;Password=xxxx;"
    }
}
```

### 3. Define Table Lists

Specify the tables to sync in the following JSON files:

#### `json/erm_table.json`

```json
{
    "TablesERM": [
        "tablename1",
        "tablename2"
    ]
}
```

#### `json/occ_table.json`

```json
{
    "TablesOCC": [
        "tablename1",
        "tablename2"
    ]
}
```

---

## ▶️ Running the Application

Run the application using the following command:

```bash
dotnet run
```

### Interactive Prompts

You will be prompted to:

1. **Choose the database to sync**:
     - `1` = ERM
     - `2` = OCC

2. **Choose the table creation option**:
     - `1` = Drop and auto-create tables
     - `2` = Use existing tables only
