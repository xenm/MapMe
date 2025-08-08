// Google Maps instance
let map;
let marker;
let clickListener;
let mapsApiLoaded = false;
// API key is provided at runtime from the server; no keys are stored in source.

// Get current position using browser's geolocation API
window.getCurrentPosition = function() {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject(new Error('Geolocation is not supported by your browser'));
            return;
        }
        
        const options = {
            enableHighAccuracy: true,
            timeout: 10000,
            maximumAge: 0
        };
        
        navigator.geolocation.getCurrentPosition(
            position => {
                resolve({
                    coords: {
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        altitude: position.coords.altitude,
                        accuracy: position.coords.accuracy,
                        altitudeAccuracy: position.coords.altitudeAccuracy,
                        heading: position.coords.heading,
                        speed: position.coords.speed
                    },
                    timestamp: position.timestamp
                });
            },
            error => {
                let errorMessage = 'Unable to retrieve your location';
                switch (error.code) {
                    case error.PERMISSION_DENIED:
                        errorMessage = 'User denied the request for geolocation';
                        break;
                    case error.POSITION_UNAVAILABLE:
                        errorMessage = 'Location information is unavailable';
                        break;
                    case error.TIMEOUT:
                        errorMessage = 'The request to get user location timed out';
                        break;
                }
                reject(new Error(errorMessage));
            },
            options
        );
    });
};

// Load Google Maps API with callback
async function loadGoogleMapsApi(apiKey) {
    if (window.google && window.google.maps) {
        mapsApiLoaded = true;
        return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
        // Create a callback function name
        const callbackName = `initMap_${Date.now()}`;
        
        // Create the script element
        const script = document.createElement('script');
        script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&libraries=places&callback=${callbackName}&loading=async`;
        script.async = true;
        script.defer = true;
        
        // Set up the global callback function
        window[callbackName] = () => {
            mapsApiLoaded = true;
            resolve();
            // Clean up
            delete window[callbackName];
            document.head.removeChild(script);
        };
        
        // Set up error handling
        script.onerror = () => {
            reject(new Error('Failed to load Google Maps API'));
            delete window[callbackName];
        };
        
        // Add the script to the document
        document.head.appendChild(script);
        
        // Set a timeout to handle cases where the API fails to load
        setTimeout(() => {
            if (!mapsApiLoaded) {
                reject(new Error('Google Maps API loading timed out'));
                delete window[callbackName];
                if (script.parentNode) {
                    document.head.removeChild(script);
                }
            }
        }, 10000); // 10 second timeout
    });
}

// Search for a location using Google Places API
function searchLocation(query) {
    return new Promise((resolve) => {
        if (!mapsApiLoaded) {
            console.error('Google Maps API not loaded');
            resolve(null);
            return;
        }

        try {
            const service = new google.maps.places.PlacesService(document.createElement('div'));
            const request = {
                query: query,
                fields: ['geometry', 'name', 'formatted_address']
            };

            service.findPlaceFromQuery(request, (results, status) => {
                if (status === google.maps.places.PlacesServiceStatus.OK && results && results[0]) {
                    const place = results[0];
                    resolve({
                        latitude: place.geometry.location.lat(),
                        longitude: place.geometry.location.lng(),
                        name: place.name,
                        address: place.formatted_address
                    });
                } else {
                    console.error('Place not found for query:', query);
                    resolve(null);
                }
            });
        } catch (error) {
            console.error('Error searching location:', error);
            resolve(null);
        }
    });
}

// Make it available globally
window.MapMe = window.MapMe || {};
window.MapMe.searchLocation = searchLocation;

// For direct function export (in case it's needed)
window.searchLocation = searchLocation;

// Export the function for ES modules
export { searchLocation };

// Initialize the map
export async function initMap(dotNetHelper, elementId, lat, lng, zoom, mapType, apiKey) {
    try {
        // Load Google Maps API if not already loaded
        if (!mapsApiLoaded) {
            await loadGoogleMapsApi(apiKey);
        }

        // Ensure the map element exists and is visible
        const mapElement = document.getElementById(elementId);
        if (!mapElement) {
            throw new Error(`Map element with ID '${elementId}' not found`);
        }

        // Create map instance
        map = new google.maps.Map(mapElement, {
            center: { lat, lng },
            zoom: zoom,
            mapTypeId: mapType,
            streetViewControl: false,
            mapTypeControl: true,
            fullscreenControl: true,
            zoomControl: true,
            disableDefaultUI: false,
            clickableIcons: true
        });

        // Function to add marker and event listeners
        const addMarker = (lat, lng, dotNetHelper) => {
            // Remove existing marker if it exists
            if (marker) {
                marker.setMap(null);
            }

            // Add new marker
            marker = new google.maps.Marker({
                position: { lat, lng },
                map: map,
                title: 'Your Location',
                animation: google.maps.Animation.DROP,
                draggable: true
            });

            // Add click event to update marker position
            if (clickListener) {
                google.maps.event.removeListener(clickListener);
            }

            clickListener = map.addListener('click', (e) => {
                const pos = e.latLng;
                marker.setPosition(pos);
                // Notify .NET about the click
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnMapClick', pos.lat(), pos.lng());
                }
            });

            // Add marker drag end event
            marker.addListener('dragend', (e) => {
                const pos = marker.getPosition();
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnMarkerDragEnd', pos.lat(), pos.lng());
                }
            });
        };
    } catch (error) {
        console.error('Error initializing map:', error);
        throw error;
    }
}

// Set map center
function setCenter(lat, lng, zoom) {
    if (map) {
        const center = new google.maps.LatLng(lat, lng);
        map.setCenter(center);
        if (marker) {
            marker.setPosition(center);
        }
        if (zoom) {
            map.setZoom(zoom);
        }
    }
}

// Make it available globally
window.MapMe = window.MapMe || {};
window.MapMe.setCenter = setCenter;

// Export for ES modules
export { setCenter };

// Removed legacy loader that embedded an API key. Keys must be provided at runtime.
