using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MapMe.Models;
using MapMe.Utils;
using Xunit;

namespace MapMe.Tests.Unit;

[Trait("Category", "Unit")]
public class DateMarkBusinessLogicTests
{
    [Fact]
    public void DateMark_Creation_SetsRequiredFields()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var geo = GeoPoint.FromLatLng(37.7749, -122.4194);

        // Act
        var dateMark = new DateMark(
            Id: "test-id",
            UserId: "user-123",
            Geo: geo,
            GeoHash: "9q8yy",
            GeoHashPrefix: "9q8",
            PlaceId: "place-123",
            PlaceSnapshot: null,
            Address: "123 Test St, San Francisco, CA",
            City: "San Francisco",
            Country: "USA",
            Categories: new[] { "Restaurant", "Italian" },
            CategoriesNorm: Normalization.ToNorm("Restaurant", "Italian"),
            Tags: new[] { "Romantic", "Cozy" },
            TagsNorm: Normalization.ToNorm("Romantic", "Cozy"),
            Qualities: new[] { "Great Food", "Good Service" },
            QualitiesNorm: Normalization.ToNorm("Great Food", "Good Service"),
            Notes: "Amazing dinner date!",
            VisitDate: new DateOnly(2025, 8, 10),
            Visibility: "public",
            CreatedAt: now,
            UpdatedAt: now,
            IsDeleted: false
        );

        // Assert
        dateMark.Should().NotBeNull();
        dateMark.Id.Should().Be("test-id");
        dateMark.UserId.Should().Be("user-123");
        dateMark.Geo.Coordinates[1].Should().Be(37.7749); 
        dateMark.Geo.Coordinates[0].Should().Be(-122.4194); 
        dateMark.Categories.Should().Contain("Restaurant");
        dateMark.CategoriesNorm.Should().Contain("restaurant");
        dateMark.CategoriesNorm.Should().Contain("italian");
        dateMark.Tags.Should().Contain("Romantic");
        dateMark.TagsNorm.Should().Contain("romantic");
        dateMark.TagsNorm.Should().Contain("cozy");
        dateMark.Qualities.Should().Contain("Great Food");
        dateMark.QualitiesNorm.Should().Contain("greatfood");
        dateMark.QualitiesNorm.Should().Contain("goodservice");
        dateMark.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void GeoPoint_FromLatLng_CreatesValidGeoPoint()
    {
        // Arrange
        var lat = 37.7749;
        var lng = -122.4194;

        // Act
        var geoPoint = GeoPoint.FromLatLng(lat, lng);

        // Assert
        geoPoint.Should().NotBeNull();
        geoPoint.Coordinates[1].Should().Be(lat);
        geoPoint.Coordinates[0].Should().Be(lng);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("friends")]
    [InlineData("private")]
    public void DateMark_Visibility_AcceptsValidValues(string visibility)
    {
        // Arrange & Act
        var dateMark = CreateTestDateMark(visibility: visibility);

        // Assert
        dateMark.Visibility.Should().Be(visibility);
    }

    [Fact]
    public void DateMark_WithPlaceSnapshot_StoresPlaceDetails()
    {
        // Arrange
        var placeSnapshot = new PlaceSnapshot(
            Name: "Test Restaurant",
            Types: new[] { "restaurant", "food" },
            Rating: 4.5,
            PriceLevel: 2
        );

        // Act
        var dateMark = CreateTestDateMark(placeSnapshot: placeSnapshot);

        // Assert
        dateMark.PlaceSnapshot.Should().NotBeNull();
        dateMark.PlaceSnapshot!.Name.Should().Be("Test Restaurant");
        dateMark.PlaceSnapshot.Rating.Should().Be(4.5);
        dateMark.PlaceSnapshot.PriceLevel.Should().Be(2);
        dateMark.PlaceSnapshot.Types.Should().Contain("restaurant");
    }

    [Fact]
    public void DateMark_NormalizationFields_AreProperlyNormalized()
    {
        // Arrange
        var categories = new[] { "Fine Dining", "Italian Restaurant" };
        var tags = new[] { "Romantic Atmosphere", "Great Wine Selection" };
        var qualities = new[] { "Excellent Service", "Amazing Food Quality" };

        // Act
        var dateMark = CreateTestDateMark(
            categories: categories,
            tags: tags,
            qualities: qualities
        );

        // Assert
        dateMark.CategoriesNorm.Should().Contain("finedining");
        dateMark.CategoriesNorm.Should().Contain("italianrestaurant");
        dateMark.TagsNorm.Should().Contain("romanticatmosphere");
        dateMark.TagsNorm.Should().Contain("greatwineselection");
        dateMark.QualitiesNorm.Should().Contain("excellentservice");
        dateMark.QualitiesNorm.Should().Contain("amazingfoodquality");
    }

    [Fact]
    public void DateMark_WithFutureVisitDate_IsValid()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));

        // Act
        var dateMark = CreateTestDateMark(visitDate: futureDate);

        // Assert
        dateMark.VisitDate.Should().Be(futureDate);
        dateMark.VisitDate.Should().BeAfter(DateOnly.FromDateTime(DateTime.Now));
    }

    [Fact]
    public void DateMark_WithNullVisitDate_IsValid()
    {
        // Act
        var dateMark = CreateTestDateMark(visitDate: null);

        // Assert
        dateMark.VisitDate.Should().BeNull();
    }

    [Fact]
    public void DateMark_UpdatedAt_CanBeModified()
    {
        // Arrange
        var originalTime = DateTimeOffset.UtcNow.AddHours(-1);
        var updatedTime = DateTimeOffset.UtcNow;
        var dateMark = CreateTestDateMark(createdAt: originalTime, updatedAt: originalTime);

        // Act
        var updatedDateMark = dateMark with { UpdatedAt = updatedTime };

        // Assert
        updatedDateMark.CreatedAt.Should().Be(originalTime);
        updatedDateMark.UpdatedAt.Should().Be(updatedTime);
        updatedDateMark.UpdatedAt.Should().BeAfter(updatedDateMark.CreatedAt);
    }

    [Fact]
    public void DateMark_SoftDelete_PreservesData()
    {
        // Arrange
        var dateMark = CreateTestDateMark();

        // Act
        var deletedDateMark = dateMark with { IsDeleted = true };

        // Assert
        deletedDateMark.IsDeleted.Should().BeTrue();
        deletedDateMark.Id.Should().Be(dateMark.Id);
        deletedDateMark.UserId.Should().Be(dateMark.UserId);
        deletedDateMark.Notes.Should().Be(dateMark.Notes);
    }

    [Theory]
    [InlineData(37.7749, -122.4194, "San Francisco")]
    [InlineData(40.7128, -74.0060, "New York")]
    [InlineData(51.5074, -0.1278, "London")]
    public void DateMark_WithDifferentLocations_StoresCorrectCoordinates(double lat, double lng, string city)
    {
        // Act
        var dateMark = CreateTestDateMark(
            geo: GeoPoint.FromLatLng(lat, lng),
            city: city
        );

        // Assert
        dateMark.Geo.Coordinates[1].Should().Be(lat);
        dateMark.Geo.Coordinates[0].Should().Be(lng);
        dateMark.City.Should().Be(city);
    }

    [Fact]
    public void DateMark_EmptyCollections_AreHandledCorrectly()
    {
        // Act
        var dateMark = CreateTestDateMark(
            categories: Array.Empty<string>(),
            tags: Array.Empty<string>(),
            qualities: Array.Empty<string>()
        );

        // Assert
        dateMark.Categories.Should().BeEmpty();
        dateMark.CategoriesNorm.Should().BeEmpty();
        dateMark.Tags.Should().BeEmpty();
        dateMark.TagsNorm.Should().BeEmpty();
        dateMark.Qualities.Should().BeEmpty();
        dateMark.QualitiesNorm.Should().BeEmpty();
    }

    private static DateMark CreateTestDateMark(
        string id = "test-id",
        string userId = "test-user",
        GeoPoint? geo = null,
        string? placeId = null,
        PlaceSnapshot? placeSnapshot = null,
        string? address = null,
        string? city = null,
        string? country = null,
        IReadOnlyList<string>? categories = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<string>? qualities = null,
        string? notes = null,
        DateOnly? visitDate = null,
        string visibility = "public",
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null,
        bool isDeleted = false)
    {
        geo ??= GeoPoint.FromLatLng(37.7749, -122.4194);
        categories ??= new[] { "Restaurant" };
        tags ??= new[] { "Romantic" };
        qualities ??= new[] { "Great Food" };
        var now = DateTimeOffset.UtcNow;
        createdAt ??= now;
        updatedAt ??= now;

        return new DateMark(
            Id: id,
            UserId: userId,
            Geo: geo,
            GeoHash: "9q8yy",
            GeoHashPrefix: "9q8",
            PlaceId: placeId,
            PlaceSnapshot: placeSnapshot,
            Address: address,
            City: city,
            Country: country,
            Categories: categories,
            CategoriesNorm: Normalization.ToNorm(categories.ToArray()),
            Tags: tags,
            TagsNorm: Normalization.ToNorm(tags.ToArray()),
            Qualities: qualities,
            QualitiesNorm: Normalization.ToNorm(qualities.ToArray()),
            Notes: notes,
            VisitDate: visitDate,
            Visibility: visibility,
            CreatedAt: createdAt.Value,
            UpdatedAt: updatedAt.Value,
            IsDeleted: isDeleted
        );
    }
}
