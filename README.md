# 🌊 SurfScout – API Backend for Surf Session & Spot Management

**SurfScout** is an ASP.NET Core-based Web API project for managing surf sessions, surf spots, and user accounts.  
The backend uses modern authentication and database technologies and is visualized on the client side through a **WPF desktop application with ArcGIS integration**.

> ⚠️ This project is under active development and serves as a hands-on learning initiative for gaining experience with C#/.NET, data analysis, geoinformation systems, and weather data integration.

---

## 🎯 Project Purpose

The quality of wave conditions at many windsurfing spots—particularly in **spatially limited coastal regions such as the southern North Sea**—is difficult to predict.  
This is largely due to such areas having a **relatively small atmospheric catchment area (“wave engine”)**, making it challenging to derive reliable forecasts for wave development.  
Factors such as wind direction, wind field size, tidal phases, timing, and local topography interact in complex ways—often beyond the capabilities of conventional forecast models.

**SurfScout** aims to address this gap by systematically collecting and storing surf sessions with geolocation, timestamps, wind, and tidal data.  
The long-term goal is to **identify historical patterns and understand how they relate to actual wave quality**—ultimately enabling **better predictions of future surf conditions** and improving decision-making for wind-based surf sessions.

---

## 🎓 Learning Goals & Motivation

This project is designed as a training platform to explore and combine the following technologies and concepts in practice:

- 🌐 **ASP.NET Core (.NET 8)** – Building secure, RESTful Web APIs  
- 🗄️ **PostgreSQL** – Data modeling and integration using Entity Framework Core  
- ☁️ **Weather APIs** – Connecting to meteorological services (e.g., wind fields, tides)  
- 📍 **ArcGIS** – Visualizing surf data and locations in the WPF client  
- 🖥️ **WPF Desktop Development** – Building GUI applications that consume APIs via C#

---

## 🚀 Current Features

- ✅ User registration and JWT-based authentication  
- ✅ Secure password hashing using BCrypt.Net  
- ✅ Endpoint protection using `[Authorize]`  
- ✅ RESTful API structure for users, surf sessions, and spots  
- ✅ PostgreSQL integration via Entity Framework Core  
- ✅ WPF client with map integration currently in progress  

---

## 🛠️ Technologies Used

| Category         | Technology                             |
|------------------|-----------------------------------------|
| Language          | C# (.NET 8)                            |
| Backend           | ASP.NET Core Web API                   |
| Authentication    | JWT (JSON Web Token), BCrypt           |
| Database          | PostgreSQL + Entity Framework Core 8   |
| Client            | WPF desktop application                |
| Geospatial        | ArcGIS (client-side integration)       |
| API Documentation | Swagger / OpenAPI                      |
