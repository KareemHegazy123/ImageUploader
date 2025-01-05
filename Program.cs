using System.Text.Json;
using System.Text.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles(); 
app.UseStaticFiles(); // Serve static files (HTML, CSS, etc.)

// JSON file to store image details
var imagesJsonPath = "images.json";

// Handle file upload
app.MapPost("/upload", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    var title = form["title"].ToString();
    var file = form.Files["image"];

    // Input validation
    if (string.IsNullOrWhiteSpace(title) || file == null || file.Length == 0)
    {
        return Results.BadRequest("Invalid input.");
    }

    // Validate file extension
    var allowedExtensions = new[] { ".jpeg", ".jpg", ".png", ".gif" };
    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!allowedExtensions.Contains(fileExtension))
    {
        return Results.BadRequest("Only JPEG, PNG, or GIF files are allowed.");
    }

    // Save the image to disk
    var imageId = Guid.NewGuid().ToString();
    var uploadsDir = Path.Combine("wwwroot", "uploads");
    var imagePath = Path.Combine(uploadsDir, $"{imageId}{fileExtension}");

    // Ensure the uploads directory exists
    Directory.CreateDirectory(uploadsDir);

    using (var stream = new FileStream(imagePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    // Save image details in JSON
    var imageInfo = new ImageInfo
    {
        Id = imageId,
        Title = title,
        Path = $"/uploads/{imageId}{fileExtension}"
    };

    var imagesList = new List<ImageInfo>();

    // Load existing images from JSON file
    if (File.Exists(imagesJsonPath))
    {
        var existingData = await File.ReadAllTextAsync(imagesJsonPath);
        imagesList = JsonSerializer.Deserialize<List<ImageInfo>>(existingData) ?? new List<ImageInfo>();
    }

    // Add the new image info to the list and save it
    imagesList.Add(imageInfo);
    await File.WriteAllTextAsync(imagesJsonPath, JsonSerializer.Serialize(imagesList));

    // Redirect to the image display page
    return Results.Redirect($"/picture/{imageId}");
});

// Display the image by ID
app.MapGet("/picture/{id}", async (string id) =>
{
    // Read image details from JSON
    if (!File.Exists(imagesJsonPath)) return Results.NotFound();

    var imagesList = JsonSerializer.Deserialize<List<ImageInfo>>(await File.ReadAllTextAsync(imagesJsonPath));
    var imageInfo = imagesList?.FirstOrDefault(img => img.Id == id);

    if (imageInfo == null) return Results.NotFound();

    // Render HTML to display image
// Render HTML to display image with a styled upload button
var html = $@"
<html>
<head>
    <title>{imageInfo.Title}</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
        }}
        .header {{
            display: flex;
            justify-content: space-between;
            align-items: center;
        }}
        .upload-button {{
            padding: 10px 20px;
            font-size: 1.2em;
            background-color: #007bff;
            color: white;
            border: none;
            border-radius: 5px;
            cursor: pointer;
        }}
        .upload-button:hover {{
            background-color: #0056b3;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>{imageInfo.Title}</h1>
        <button class='upload-button' onclick=""window.location.href='/'"">Upload New Picture</button>
    </div>
    <img src='{imageInfo.Path}' alt='{imageInfo.Title}' />
</body>
</html>";


    return Results.Content(html, "text/html");
});

app.Run();
