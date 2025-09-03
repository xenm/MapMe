// Google Maps instance
let map;
let marker;
let clickListener;
let placeClickListener;
let mapsApiLoaded = false;
// Keep track of rendered saved markers
let savedMarkers = [];
let sharedInfoWindow = null;
// API key is provided at runtime from the server; no keys are stored in source.

// Get current position using browser's geolocation API
window.getCurrentPosition = function () {
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
export {searchLocation};

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
            center: {lat, lng},
            zoom: zoom,
            mapTypeId: mapType,
            streetViewControl: false,
            mapTypeControl: true,
            fullscreenControl: true,
            zoomControl: true,
            disableDefaultUI: false,
            clickableIcons: true
        });

        // Small prompt before opening the full dialog
        const showDateProposalPrompt = ({position, title, address, photos, url, isAdvanced = false, onConfirm}) => {
            try {
                if (!sharedInfoWindow) {
                    sharedInfoWindow = new google.maps.InfoWindow();
                }
                const safe = (s) => typeof s === 'string' ? s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;') : '';
                const list = Array.isArray(photos) && photos.length > 0 ? photos : ['/images/place-photo.svg'];
                const imgs = list
                    .map(u => `<img class=\"mm-thumb\" src=\"${u}\" style=\"width:72px;height:72px;border-radius:6px;object-fit:cover;border:1px solid #e9ecef;cursor:pointer;\"/>`)
                    .join('');

                // Make title clickable if URL is available
                const titleHtml = title ?
                    (url ?
                        `<div style=\"font-weight:600;margin-bottom:2px;\"><a href=\"${safe(url)}\" target=\"_blank\" rel=\"noopener noreferrer\" style=\"color:#0d6efd;text-decoration:none;\">${safe(title)}<svg style=\"width:12px;height:12px;margin-left:4px;vertical-align:baseline;\" viewBox=\"0 0 24 24\" fill=\"currentColor\"><path d=\"M14,3V5H17.59L7.76,14.83L9.17,16.24L19,6.41V10H21V3M19,19H5V5H12V3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V12H19V19Z\" /></svg></a></div>` :
                        `<div style=\"font-weight:600;margin-bottom:2px;\">${safe(title)}</div>`) :
                    '';

                const content = `
                  <div style="min-width:220px; max-width:320px;">
                    ${titleHtml}
                    ${address ? `<div style=\"color:#6c757d;font-size:12px;margin-bottom:6px;\">${safe(address)}</div>` : ''}
                    <div class=\"mm-scroll\" style=\"display:flex; gap:8px; overflow-x:auto; padding-bottom:4px; margin:6px 0;\">${imgs}</div>
                    <div style="display:flex; gap:8px; align-items:center;">
                      <button id="mm-mini-create" style="display:inline-flex;align-items:center;gap:6px;padding:4px 8px;border:0;border-radius:4px;background:#3478f6;color:#fff;font-size:12px;cursor:pointer;">
                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M12 5v14M5 12h14" stroke="white" stroke-width="2" stroke-linecap="round"/></svg>
                        Create
                      </button>
                      <button id="mm-mini-cancel" style="padding:4px 8px;border:1px solid #dee2e6;border-radius:4px;background:#fff;color:#333;font-size:12px;cursor:pointer;">Cancel</button>
                    </div>
                  </div>`;
                sharedInfoWindow.setContent(content);
                // Ensure we are not anchored to a previous marker; explicitly set position and open on map
                try {
                    sharedInfoWindow.close();
                } catch (_) {
                }
                sharedInfoWindow.setPosition(position);
                sharedInfoWindow.open({map});
                // Wire events after the DOM is attached
                // Use a short timeout to ensure DOM is available
                setTimeout(() => {
                    const createBtn = document.getElementById('mm-mini-create');
                    const cancelBtn = document.getElementById('mm-mini-cancel');
                    // Lightbox for place photos in prompt
                    try {
                        const container = document.querySelector('.gm-style-iw, .gm-style-iw-c')?.parentElement || document.body;
                        const thumbs = container.querySelectorAll('.mm-thumb');
                        const urls = list.slice();
                        thumbs.forEach((el, idx) => {
                            el.addEventListener('click', () => {
                                try {
                                    window.MapMe && typeof window.MapMe.openPhotoViewer === 'function' ? window.MapMe.openPhotoViewer(urls, idx) : openPhotoViewer(urls, idx);
                                } catch (_) {
                                }
                            }, {once: true});
                        });
                        // Handle Google Maps links in creation popup
                        const titleLinks = container.querySelectorAll('a[href*="google"], a[href*="maps"], a[target="_blank"]');
                        titleLinks.forEach(link => {
                            link.addEventListener('click', (e) => {
                                e.stopPropagation(); // Prevent popup from closing
                                // Let the default link behavior proceed (opening in new tab)
                            });
                        });
                    } catch (_) {
                    }
                    if (createBtn) {
                        createBtn.onclick = () => {
                            try {
                                sharedInfoWindow.close();
                            } catch (_) {
                            }
                            onConfirm && onConfirm();
                        };
                    }
                    if (cancelBtn) {
                        cancelBtn.onclick = () => {
                            try {
                                sharedInfoWindow.close();
                            } catch (_) {
                            }
                        };
                    }
                }, 0);
            } catch (e) {
                console.error('showDateProposalPrompt error', e);
            }
        };

        // Function to add marker and event listeners
        const addMarker = (lat, lng, dotNetHelper) => {
            // Remove existing marker if it exists
            if (marker) {
                marker.setMap(null);
            }

            // Create a transparent 1x1 pixel PNG for the invisible marker
            const invisibleIcon = {
                url: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=',
                size: new google.maps.Size(1, 1),
                origin: new google.maps.Point(0, 0),
                anchor: new google.maps.Point(0, 0)
            };

            // Add new marker (invisible and non-draggable)
            marker = new google.maps.Marker({
                position: {lat, lng},
                map: map,
                title: '',
                draggable: false,
                visible: true,
                icon: invisibleIcon,
                clickable: false
            });

            // Remove existing click listeners
            if (clickListener) {
                google.maps.event.removeListener(clickListener);
            }
            if (placeClickListener) {
                google.maps.event.removeListener(placeClickListener);
            }

            // Module-level variable to track advanced dialog state
            let isInAdvancedDialog = false;

            // Function to handle map clicks
            const handleMapClick = (e) => {
                try {
                    console.debug('Map click at', e.latLng && e.latLng.toString(), 'placeId:', e.placeId);
                } catch (_) {
                }
                try {
                    // Check if we're in an advanced dialog by looking for specific class
                    isInAdvancedDialog = document.querySelector('.advanced-dialog') !== null;

                    // If a dialog/info window is currently open, check if we should close it
                    if (sharedInfoWindow && typeof sharedInfoWindow.getMap === 'function' && sharedInfoWindow.getMap()) {
                        if (!isInAdvancedDialog) {
                            try {
                                sharedInfoWindow.close();
                            } catch (_) {
                            }
                        }
                        // If we have an advanced dialog, don't proceed with other click handling
                        if (isInAdvancedDialog) {
                            e.stop();
                            return;
                        }
                    }
                    // Prevent default behavior for all clicks
                    e.stop();

                    // If user clicked a POI on the map, e.placeId will be present
                    if (e.placeId) {
                        // Prevent default Maps behavior
                        e.stop();
                        const service = new google.maps.places.PlacesService(map);
                        const fields = [
                            'place_id', 'name', 'geometry', 'types', 'url', 'photos', 'formatted_address'
                        ];
                        service.getDetails({placeId: e.placeId, fields}, (place, status) => {
                            if (status === google.maps.places.PlacesServiceStatus.OK && place) {
                                try {
                                    const loc = place.geometry && place.geometry.location
                                        ? {lat: place.geometry.location.lat(), lng: place.geometry.location.lng()}
                                        : {lat: lat, lng: lng};
                                    // Build a portable details object
                                    // NOTE: JS Places Photo object does not expose photo_reference.
                                    // If you need stable references, use Places Details Web Service on the server
                                    // to get photo_reference values and generate URLs when rendering.
                                    const photoReferences = [];
                                    let placePhotoUrl = null;
                                    let photoUrls = [];
                                    try {
                                        if (place.photos && place.photos.length > 0) {
                                            photoUrls = place.photos.map(p => {
                                                try {
                                                    return typeof p.getUrl === 'function' ? p.getUrl({
                                                        maxWidth: 800,
                                                        maxHeight: 600
                                                    }) : null;
                                                } catch (_) {
                                                    return null;
                                                }
                                            }).filter(Boolean).slice(0, 12);
                                            if (photoUrls.length > 0) {
                                                placePhotoUrl = photoUrls[0];
                                            }
                                        }
                                    } catch (_) { /* ignore */
                                    }
                                    const details = {
                                        placeId: place.place_id || e.placeId,
                                        name: place.name || null,
                                        location: loc,
                                        types: place.types || [],
                                        url: place.url || null,
                                        photoReferences: photoReferences,
                                        address: place.formatted_address || null,
                                        placePhotoUrl: placePhotoUrl,
                                        placePhotoUrls: photoUrls
                                    };
                                    // Show pre-confirm prompt
                                    showDateProposalPrompt({
                                        position: new google.maps.LatLng(loc.lat, loc.lng),
                                        title: details.name || 'Selected place',
                                        address: details.address || '',
                                        photos: photoUrls,
                                        url: details.url,
                                        isAdvanced: false, // Mark as non-advanced dialog
                                        onConfirm: () => {
                                            // Move marker after confirm
                                            marker.setPosition(new google.maps.LatLng(loc.lat, loc.lng));
                                            if (dotNetHelper) {
                                                dotNetHelper.invokeMethodAsync('OnPlaceDetailsAsync', details);
                                            }
                                        }
                                    });
                                } catch (err) {
                                    console.error('Error processing place details', err);
                                }
                            } else {
                                // Fallback: act like a raw map click
                                const pos = e.latLng;
                                // Reverse geocode minimal info then prompt (try to fetch photos if placeId exists)
                                reverseGeocode(pos.lat(), pos.lng()).then(min => {
                                    const title = (min && min.name) ? min.name : 'Selected location';
                                    const address = (min && min.address) ? min.address : '';
                                    const pid = min && min.placeId ? min.placeId : null;
                                    if (pid) {
                                        const service2 = new google.maps.places.PlacesService(map);
                                        service2.getDetails({placeId: pid, fields: ['photos']}, (pl, st) => {
                                            let thumbs = [];
                                            try {
                                                if (st === google.maps.places.PlacesServiceStatus.OK && pl && pl.photos && pl.photos.length) {
                                                    thumbs = pl.photos.slice(0, 3).map(p => {
                                                        try {
                                                            return typeof p.getUrl === 'function' ? p.getUrl({
                                                                maxWidth: 400,
                                                                maxHeight: 300
                                                            }) : null;
                                                        } catch (_) {
                                                            return null;
                                                        }
                                                    }).filter(Boolean);
                                                }
                                            } catch (_) {
                                            }
                                            showDateProposalPrompt({
                                                position: pos,
                                                title,
                                                address,
                                                photos: thumbs,
                                                url: null,
                                                onConfirm: () => {
                                                    marker.setPosition(pos);
                                                    if (dotNetHelper) {
                                                        dotNetHelper.invokeMethodAsync('OnMapClickAsync', pos.lat(), pos.lng());
                                                    }
                                                }
                                            });
                                        });
                                    } else {
                                        showDateProposalPrompt({
                                            position: pos, title, address, photos: [], url: null, onConfirm: () => {
                                                marker.setPosition(pos);
                                                if (dotNetHelper) {
                                                    dotNetHelper.invokeMethodAsync('OnMapClickAsync', pos.lat(), pos.lng());
                                                }
                                            }
                                        });
                                    }
                                });
                            }
                        });
                        return; // handled via details path
                    }
                } catch (err) {
                    console.debug('Error handling map click:', err);
                }
            };

            // Add click listener for regular map clicks (single click to close dialogs)
            clickListener = map.addListener('click', (e) => {
                try {
                    // If there's a place ID, handle it as a place click
                    if (e.placeId) {
                        handleMapClick(e);
                        return;
                    }

                    // For regular map clicks, close any open dialogs
                    const isAdvancedDialog = document.querySelector('.advanced-dialog') !== null;
                    if (sharedInfoWindow && !isAdvancedDialog) {
                        try {
                            sharedInfoWindow.close();
                        } catch (_) {
                        }
                    }
                } catch (err) {
                    console.error('Error handling map click:', err);
                }
            });

            // Add double-click listener for creating new marks
            map.addListener('dblclick', (e) => {
                try {
                    // Prevent default double-click zoom behavior
                    e.stop();

                    // Only handle if not a place click (those are handled by single click)
                    if (!e.placeId) {
                        // For empty map locations, directly show the prompt with the clicked position
                        const pos = e.latLng;
                        showDateProposalPrompt({
                            position: pos,
                            title: 'Selected location',
                            address: '',
                            photos: [],
                            url: null,
                            onConfirm: () => {
                                marker.setPosition(pos);
                                if (dotNetHelper) {
                                    dotNetHelper.invokeMethodAsync('OnMapClickAsync', pos.lat(), pos.lng());
                                }
                            }
                        });
                    }
                } catch (err) {
                    console.error('Error handling double-click:', err);
                }
            });

            // Add marker drag end event
            marker.addListener('dragend', (e) => {
                const pos = marker.getPosition();
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnMarkerDragEndAsync', pos.lat(), pos.lng());
                }
            });
        };

        // Ensure we add the initial marker and listeners
        addMarker(lat, lng, dotNetHelper);
    } catch (error) {
        console.error('Error initializing map:', error);
        throw error;
    }
}

// Provide a no-op dispose to be safely called from .NET during teardown
export function dispose() {
    try {
        if (clickListener) {
            google.maps.event.removeListener(clickListener);
            clickListener = null;
        }
        if (marker) {
            marker.setMap(null);
            marker = null;
        }
        // Clear any saved markers
        if (savedMarkers && savedMarkers.length) {
            savedMarkers.forEach(m => m.setMap(null));
            savedMarkers = [];
        }
        if (sharedInfoWindow) {
            try {
                sharedInfoWindow.close();
            } catch (_) {
            }
            sharedInfoWindow = null;
        }
        // We intentionally do not call any DOM operations if map element is gone
    } catch (e) {
        // Swallow any errors during dispose
        console.debug('dispose() ignored an error:', e);
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
export {setCenter};

// Removed legacy loader that embedded an API key. Keys must be provided at runtime.

// Reverse geocode a coordinate to a friendly name/address
async function reverseGeocode(lat, lng) {
    if (!mapsApiLoaded) {
        console.error('Google Maps API not loaded');
        return null;
    }
    try {
        const geocoder = new google.maps.Geocoder();
        const latlng = {lat: Number(lat), lng: Number(lng)};
        return await new Promise((resolve) => {
            geocoder.geocode({location: latlng}, (results, status) => {
                if (status === 'OK' && results && results.length > 0) {
                    const best = results[0];
                    resolve({
                        name: best.address_components?.find(c => c.types.includes('point_of_interest'))?.long_name || best.formatted_address,
                        address: best.formatted_address,
                        placeId: best.place_id || null
                    });
                } else {
                    resolve(null);
                }
            });
        });
    } catch (e) {
        console.error('reverseGeocode error', e);
        return null;
    }
}

// Simple localStorage helpers
window.MapMe = window.MapMe || {};
window.MapMe.storage = {
    save: (key, value) => {
        try {
            localStorage.setItem(key, value);
        } catch (_) {
        }
    },
    load: (key) => {
        try {
            return localStorage.getItem(key);
        } catch (_) {
            return null;
        }
    }
};

// Make helpers available on window and as module exports
window.MapMe.reverseGeocode = reverseGeocode;
export {reverseGeocode};

// Function to show popup for a specific location (called from Blazor)
function showPopupForLocation(lat, lng, placeId) {
    if (!mapsApiLoaded || !map || !savedMarkers) {
        console.warn('Map not ready for showing popup');
        return;
    }

    try {
        // Find the marker that matches this location and placeId
        const targetMarker = savedMarkers.find(marker => {
            if (!marker.labelOverlay) return false;

            // Check if this marker is at the same location (within small tolerance)
            const markerPos = marker.getPosition();
            if (!markerPos) return false;

            const latDiff = Math.abs(markerPos.lat() - lat);
            const lngDiff = Math.abs(markerPos.lng() - lng);
            const tolerance = 0.0001; // ~10 meters

            return latDiff < tolerance && lngDiff < tolerance;
        });

        if (targetMarker && targetMarker.labelOverlay) {
            // Simulate a click on the marker to show the popup
            const container = targetMarker.labelOverlay.div;
            if (container) {
                // Trigger the click event that shows the popup
                const clickEvent = new MouseEvent('click', {
                    bubbles: true,
                    cancelable: true,
                    view: window
                });
                container.dispatchEvent(clickEvent);
            }
        } else {
            console.warn('Could not find marker for location:', lat, lng, placeId);
        }
    } catch (error) {
        console.error('Error showing popup for location:', error);
    }
}

// Make it available globally
window.MapMe = window.MapMe || {};
window.MapMe.showPopupForLocation = showPopupForLocation;
export {showPopupForLocation};

// Render a collection of saved marks on the map using user's first photo as the marker icon
function renderMarks(marks) {
    if (!mapsApiLoaded || !map) {
        // Map not ready yet; ignore safely
        return;
    }
    try {
        // Remove existing saved markers
        if (savedMarkers && savedMarkers.length) {
            savedMarkers.forEach(m => {
                try {
                    if (m.labelOverlay && typeof m.labelOverlay.setMap === 'function') {
                        m.labelOverlay.setMap(null);
                        m.labelOverlay = null;
                    }
                } catch (_) { /* ignore */
                }
                try {
                    m.setMap(null);
                } catch (_) { /* ignore */
                }
            });
        }
        savedMarkers = [];

        if (!Array.isArray(marks) || marks.length === 0) {
            return;
        }

        // Reuse a single info window
        if (!sharedInfoWindow) {
            sharedInfoWindow = new google.maps.InfoWindow();
        }

        // Group marks by placeId first; fallback to proximity (<= ~25m)
        const groups = [];
        const AVATAR = '/images/user-avatar.svg';
        const PLACE_FALLBACK = '/images/place-photo.svg';
        const toNum = v => (v === null || v === undefined || v === '' ? null : Number(v));
        const haversineMeters = (a, b) => {
            const R = 6371000; // meters
            const dLat = (b.lat - a.lat) * Math.PI / 180;
            const dLng = (b.lng - a.lng) * Math.PI / 180;
            const s1 = Math.sin(dLat / 2);
            const s2 = Math.sin(dLng / 2);
            const aa = s1 * s1 + Math.cos(a.lat * Math.PI / 180) * Math.cos(b.lat * Math.PI / 180) * s2 * s2;
            const c = 2 * Math.atan2(Math.sqrt(aa), Math.sqrt(1 - aa));
            return R * c;
        };
        const findGroupIndex = (mark) => {
            const pid = mark.placeId || mark.place_id || null;
            if (pid) {
                const idx = groups.findIndex(g => g.placeId && g.placeId === pid);
                if (idx !== -1) return idx;
            }
            // proximity fallback (~25m)
            const pos = {lat: toNum(mark.lat), lng: toNum(mark.lng)};
            for (let i = 0; i < groups.length; i++) {
                if (!groups[i].placeId) {
                    if (haversineMeters(pos, groups[i].pos) <= 25) return i;
                }
            }
            return -1;
        };

        marks.forEach(m => {
            const pos = {lat: toNum(m.lat), lng: toNum(m.lng)};
            const idx = findGroupIndex(m);
            // Collect photos
            const userArr = [];
            if (Array.isArray(m.userPhotoUrls) && m.userPhotoUrls.length > 0) userArr.push(...m.userPhotoUrls);
            if (Array.isArray(m.userPhotos) && m.userPhotos.length > 0) userArr.push(...m.userPhotos);
            if (m.userPhotoUrl) userArr.push(m.userPhotoUrl);
            let userPhotos = [...new Set(userArr.filter(Boolean))];
            if (userPhotos.length > 1) userPhotos = userPhotos.filter(u => u !== AVATAR);
            if (!userPhotos.length) userPhotos.push(AVATAR);

            const placeArr = [];
            if (Array.isArray(m.placePhotoUrls) && m.placePhotoUrls.length > 0) placeArr.push(...m.placePhotoUrls);
            if (Array.isArray(m.placePhotos) && m.placePhotos.length > 0) placeArr.push(...m.placePhotos);
            if (m.placePhotoUrl) placeArr.push(m.placePhotoUrl);
            let placePhotos = [...new Set(placeArr.filter(Boolean))];
            if (!placePhotos.length) placePhotos.push(PLACE_FALLBACK);

            const entry = {
                mark: m,
                userPhotos,
                placePhotos,
                createdBy: m.createdBy || null,
                title: m.title || m.name || null,
                address: m.address || null,
                url: m.url || null
            };

            if (idx === -1) {
                groups.push({
                    placeId: m.placeId || m.place_id || null,
                    pos: {...pos},
                    items: [entry]
                });
            } else {
                const g = groups[idx];
                g.items.push(entry);
                // update centroid for proximity groups
                if (!g.placeId) {
                    const n = g.items.length;
                    g.pos = {
                        lat: g.pos.lat + (pos.lat - g.pos.lat) / n,
                        lng: g.pos.lng + (pos.lng - g.pos.lng) / n
                    };
                }
            }
        });

        // Render one overlay per group
        groups.forEach(g => {
            const pos = g.pos;
            // Determine base place photo
            let basePlace = PLACE_FALLBACK;
            for (const it of g.items) {
                if (it.placePhotos && it.placePhotos[0]) {
                    basePlace = it.placePhotos[0];
                    break;
                }
            }
            // Build user list (unique by createdBy if available)
            const users = [];
            const seenUsers = new Set();
            g.items.forEach(it => {
                const key = it.createdBy || (it.userPhotos && it.userPhotos[0]) || Math.random().toString(36).slice(2);
                if (!seenUsers.has(key)) {
                    seenUsers.add(key);
                    users.push({
                        name: it.createdBy || null,
                        photo: (it.userPhotos && it.userPhotos[0]) || AVATAR
                    });
                }
            });

            const transparentIcon = {
                url: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAEAAAAAwCAYAAAB9DqNwAAAAC0lEQVR4nO3BMQEAAADCoPdPbQ43oAAAAAAK4wAAAKS3XrQAAQ==',
                size: new google.maps.Size(64, 64),
                scaledSize: new google.maps.Size(64, 64),
                origin: new google.maps.Point(0, 0),
                anchor: new google.maps.Point(32, 56)
            };

            const mk = new google.maps.Marker({
                position: pos,
                map: map,
                icon: transparentIcon,
                title: g.items[0]?.title || 'Saved place',
                clickable: true,
                optimized: false,
                zIndex: 100
            });

            // Overlay container sized to fit base + chips
            const container = document.createElement('div');
            container.style.position = 'absolute';
            container.style.width = '72px';
            container.style.height = '64px';
            container.style.pointerEvents = 'auto';
            container.style.cursor = 'pointer';
            container.style.zIndex = '9999';

            // Base place circle 48px centered near bottom
            const base = document.createElement('div');
            base.style.position = 'absolute';
            base.style.left = '12px';
            base.style.top = '16px';
            base.style.width = '48px';
            base.style.height = '48px';
            base.style.borderRadius = '50%';
            base.style.backgroundImage = `url(${basePlace})`;
            base.style.backgroundSize = 'cover';
            base.style.backgroundPosition = 'center';
            base.style.boxShadow = '0 2px 4px rgba(0,0,0,0.25)';
            base.style.overflow = 'hidden';
            base.style.zIndex = '1';
            container.appendChild(base);

            // Hover label (name/title) shown above the icon
            const distinctNames = Array.from(new Set(g.items.map(it => it.createdBy).filter(Boolean)));
            // Prefer the place name/title whenever available
            const getPlaceTitle = () => {
                for (const it of g.items) {
                    const t = it.title || (it.mark && (it.mark.placeName || it.mark.name || it.mark.title));
                    if (t) return t;
                }
                return null;
            };
            let displayLabel = getPlaceTitle();
            if (!displayLabel) {
                if (distinctNames.length > 1) displayLabel = `${distinctNames.length} users`;
                else if (distinctNames.length === 1) displayLabel = distinctNames[0];
                else displayLabel = 'Saved place';
            }

            const nameLabel = document.createElement('div');
            nameLabel.textContent = displayLabel;
            nameLabel.style.position = 'absolute';
            nameLabel.style.left = '50%';
            nameLabel.style.top = '-6px';
            nameLabel.style.transform = 'translateX(-50%)';
            nameLabel.style.maxWidth = '160px';
            nameLabel.style.whiteSpace = 'nowrap';
            nameLabel.style.textOverflow = 'ellipsis';
            nameLabel.style.overflow = 'hidden';
            nameLabel.style.padding = '2px 6px';
            nameLabel.style.fontSize = '12px';
            nameLabel.style.fontWeight = '600';
            nameLabel.style.color = '#111827';
            nameLabel.style.background = 'rgba(255,255,255,0.95)';
            nameLabel.style.border = '1px solid #e5e7eb';
            nameLabel.style.borderRadius = '6px';
            nameLabel.style.boxShadow = '0 2px 8px rgba(0,0,0,0.15)';
            nameLabel.style.pointerEvents = 'none';
            nameLabel.style.opacity = '0';
            nameLabel.style.transition = 'opacity 120ms ease';
            nameLabel.style.zIndex = '4';
            container.appendChild(nameLabel);

            // Helper to create a chip
            const makeChip = (sizePx, leftPx, topPx, photoUrl, isCounter = false, text = '') => {
                const chip = document.createElement('div');
                chip.style.position = 'absolute';
                chip.style.width = `${sizePx}px`;
                chip.style.height = `${sizePx}px`;
                chip.style.left = `${leftPx}px`;
                chip.style.top = `${topPx}px`;
                chip.style.borderRadius = '50%';
                chip.style.boxShadow = '0 4px 8px rgba(0,0,0,0.35), 0 0 0 2px #fff';
                chip.style.overflow = 'hidden';
                chip.style.zIndex = '3';
                if (isCounter) {
                    chip.style.background = '#2b2f36';
                    chip.style.color = '#fff';
                    chip.style.display = 'flex';
                    chip.style.alignItems = 'center';
                    chip.style.justifyContent = 'center';
                    chip.style.fontSize = '12px';
                    chip.style.fontWeight = '700';
                    chip.textContent = text;
                } else {
                    chip.style.backgroundImage = `url(${photoUrl})`;
                    chip.style.backgroundSize = 'cover';
                    chip.style.backgroundPosition = 'center';
                }
                return chip;
            };

            // Place up to 3 user chips bottom-right cascade
            const maxChips = 3;
            const extra = Math.max(0, users.length - maxChips);
            const chips = users.slice(0, maxChips).map(u => u.photo);
            // Sizes and offsets relative to container
            const layout = [
                {size: 20, left: 40, top: 36},
                {size: 24, left: 32, top: 28},
                {size: 28, left: 24, top: 20}
            ];
            for (let i = 0; i < chips.length; i++) {
                const cfg = layout[i];
                const node = (extra > 0 && i === chips.length - 1)
                    ? makeChip(cfg.size, cfg.left, cfg.top, '', true, extra > 99 ? '99+' : `+${extra}`)
                    : makeChip(cfg.size, cfg.left, cfg.top, chips[i]);
                container.appendChild(node);
            }

            const labelOverlay = new google.maps.OverlayView();
            labelOverlay.onAdd = function () {
                this.div = container;
                const panes = this.getPanes();
                panes.overlayMouseTarget.appendChild(this.div);
                try {
                    const handler = (ev) => {
                        try {
                            ev && ev.preventDefault && ev.preventDefault();
                        } catch (_) {
                        }
                        try {
                            ev && ev.stopPropagation && ev.stopPropagation();
                        } catch (_) {
                        }
                        try {
                            showGroupInfo();
                        } catch (_) {
                        }
                    };
                    const handlerDown = (ev) => {
                        try {
                            ev && ev.preventDefault && ev.preventDefault();
                            ev && ev.stopPropagation && ev.stopPropagation();
                        } catch (_) {
                        }
                    };
                    this._listeners = [
                        google.maps.event.addDomListener(this.div, 'click', handler),
                        google.maps.event.addDomListener(this.div, 'mousedown', handlerDown),
                        google.maps.event.addDomListener(this.div, 'mouseup', handler),
                        google.maps.event.addDomListener(this.div, 'touchstart', handlerDown),
                        google.maps.event.addDomListener(this.div, 'touchend', handler),
                        // Hover to show/hide name label
                        google.maps.event.addDomListener(this.div, 'mouseenter', () => {
                            try {
                                nameLabel.style.opacity = '1';
                            } catch (_) {
                            }
                        }),
                        google.maps.event.addDomListener(this.div, 'mouseleave', () => {
                            try {
                                nameLabel.style.opacity = '0';
                            } catch (_) {
                            }
                        })
                    ];
                } catch (_) { /* ignore */
                }
            };
            labelOverlay.draw = function () {
                const projection = this.getProjection();
                const position = projection.fromLatLngToDivPixel(pos);
                if (position) {
                    container.style.left = (position.x - 36) + 'px';
                    container.style.top = (position.y - 64) + 'px';
                }
            };
            labelOverlay.onRemove = function () {
                try {
                    if (this._listeners && this._listeners.length) {
                        this._listeners.forEach(l => {
                            try {
                                google.maps.event.removeListener(l);
                            } catch (_) {
                            }
                        });
                        this._listeners = [];
                    }
                    if (this._mapListeners && this._mapListeners.length) {
                        this._mapListeners.forEach(l => {
                            try {
                                google.maps.event.removeListener(l);
                            } catch (_) {
                            }
                        });
                        this._mapListeners = [];
                    }
                    if (this.div && this.div.parentNode) {
                        this.div.parentNode.removeChild(this.div);
                    }
                    this.div = null;
                } catch (_) { /* ignore */
                }
            };
            labelOverlay.setMap(map);
            mk.labelOverlay = labelOverlay;
            const redraw = () => {
                try {
                    labelOverlay.draw();
                } catch (_) {
                }
            };
            labelOverlay._mapListeners = [
                google.maps.event.addListener(map, 'bounds_changed', redraw),
                google.maps.event.addListener(map, 'idle', redraw),
                google.maps.event.addListener(map, 'zoom_changed', redraw),
                google.maps.event.addListener(map, 'drag', redraw)
            ];
            try {
                google.maps.event.addListenerOnce(map, 'idle', redraw);
            } catch (_) {
            }

            const showGroupInfo = () => {
                try {
                    console.debug('Opening mark info at', pos);
                } catch (_) {
                }
                const firstWith = (prop) => {
                    for (const it of g.items) if (it[prop]) return it[prop];
                    return null;
                };
                const titleVal = firstWith('title');
                const addrVal = firstWith('address');
                const urlVal = firstWith('url') || (g.items[0] && g.items[0].mark && g.items[0].mark.url);

                const title = titleVal ?
                    (urlVal ?
                        `<div style=\"font-weight:600;\"><a href=\"${escapeHtml(urlVal)}\" target=\"_blank\" rel=\"noopener noreferrer\" style=\"color:#0d6efd;text-decoration:none;\">${escapeHtml(titleVal)}<svg style=\"width:12px;height:12px;margin-left:4px;vertical-align:baseline;\" viewBox=\"0 0 24 24\" fill=\"currentColor\"><path d=\"M14,3V5H17.59L7.76,14.83L9.17,16.24L19,6.41V10H21V3M19,19H5V5H12V3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V12H19V19Z\" /></svg></a></div>` :
                        `<div style=\"font-weight:600;\">${escapeHtml(titleVal)}</div>`) :
                    '';
                const addr = addrVal ? `<div style=\"color:#6c757d;font-size:12px;margin-bottom:6px;\">${escapeHtml(addrVal)}</div>` : '';
                const thumbHtml = (url) => `<img class=\"mm-thumb\" src=\"${url}\" alt=\"\" style=\"width:72px;height:72px;border-radius:8px;object-fit:cover;border:1px solid #e9ecef;cursor:pointer;\"/>`;
                // Build sections: first place images, then for each user: their images, name link and message
                const placeUrls = [...new Set(g.items.flatMap(it => it.placePhotos).filter(Boolean))];
                const byUser = new Map(); // name -> { urls:Set, messages:Set, avatar:string, dateMarks:[] }
                for (const it of g.items) {
                    const name = it.createdBy || 'Unknown';
                    if (!byUser.has(name)) byUser.set(name, {
                        urls: new Set(),
                        messages: new Set(),
                        avatar: null,
                        dateMarks: []
                    });
                    const entry = byUser.get(name);
                    (it.userPhotos || []).forEach(u => {
                        if (u) entry.urls.add(u);
                    });
                    const src = it.mark || {};
                    const msg = src.message || src.userMessage || src.note || src.comment || src.caption || src.description || src.text || src.msg || null;
                    if (msg) entry.messages.add(msg);
                    if (!entry.avatar) entry.avatar = (it.userPhotos && it.userPhotos[0]) || src.userPhotoUrl || '/images/user-avatar.svg';
                    // Store the full DateMark data for editing
                    entry.dateMarks.push(src);
                }
                const userSections = Array.from(byUser.entries()).map(([name, val]) => ({
                    name,
                    urls: Array.from(val.urls),
                    messages: Array.from(val.messages),
                    avatar: val.avatar,
                    dateMarks: val.dateMarks
                }));
                // Optional: sort users alphabetically for consistent order
                userSections.sort((a, b) => a.name.localeCompare(b.name));

                const sections = [];
                if (placeUrls.length) sections.push({type: 'place', label: 'Place photos', urls: placeUrls});
                for (const us of userSections) {
                    sections.push({
                        type: 'user',
                        label: us.name,
                        urls: us.urls,
                        messages: us.messages,
                        avatar: us.avatar,
                        dateMarks: us.dateMarks
                    });
                }

                const sectionsHtml = sections.map((sec, idx) => {
                    const heading = sec.type === 'user'
                        ? `<div style=\"display:flex;align-items:center;gap:6px;margin-top:${idx === 0 ? '0' : '8'}px;\"><span style=\"font-weight:600;\">User:</span> <a href=\"/user/${encodeURIComponent(sec.label)}\" class=\"mm-user-link\" data-username=\"${encodeURIComponent(sec.label)}\" data-avatar=\"${sec.avatar}\" style=\"text-decoration:none;\">${escapeHtml(sec.label)}</a></div>`
                        : `<div style=\"font-weight:600;margin-top:${idx === 0 ? '0' : '8'}px;\">${escapeHtml(sec.label)}</div>`;
                    let msgHtml = '';
                    if (sec.type === 'user' && Array.isArray(sec.messages) && sec.messages.length) {
                        const items = sec.messages.map(m => `<li>${escapeHtml(m)}</li>`).join('');
                        msgHtml = `<ul style=\"color:#6b7280;font-size:12px;margin:2px 0 4px;padding-left:16px;\">${items}</ul>`;
                    }
                    // Add edit button for current user's Date Marks
                    let editButtonHtml = '';
                    if (sec.type === 'user' && sec.dateMarks && sec.dateMarks.length > 0) {
                        // Check if this is the current user (we'll use a simple check for now)
                        const isCurrentUser = window.MapMe && window.MapMe.currentUser && window.MapMe.currentUser === sec.label;
                        if (isCurrentUser) {
                            const dateMarkId = sec.dateMarks[0].id || sec.dateMarks[0].Id;
                            if (dateMarkId) {
                                editButtonHtml = `<div style=\"margin:6px 0;\"><button class=\"mm-edit-btn\" data-datemark-id=\"${dateMarkId}\" style=\"background:#007bff;color:white;border:none;padding:4px 8px;border-radius:4px;font-size:12px;cursor:pointer;\">✏️ Edit Date Mark</button></div>`;
                            }
                        }
                    }
                    const strip = `<div class=\"mm-scroll\" data-sec-idx=\"${idx}\" style=\"display:flex; gap:8px; overflow-x:auto; padding-bottom:4px; margin:6px 0;\">${sec.urls.map(thumbHtml).join('')}</div>`;
                    return `<div class=\"mm-sec\">${heading}${msgHtml}${editButtonHtml}${strip}</div>`;
                }).join('');
                const content = `<div style=\"max-width:320px; max-height:360px; overflow:auto;\">${title}${addr}${sectionsHtml}</div>`;
                try {
                    sharedInfoWindow.close();
                } catch (_) {
                }
                sharedInfoWindow.setContent(content);
                // Anchor to the marker for precise placement
                sharedInfoWindow.open({map, anchor: mk});
                // After open, attach click handlers for lightbox
                setTimeout(() => {
                    try {
                        const container = document.querySelector('.gm-style-iw, .gm-style-iw-c')?.parentElement || document.body;
                        const strips = container.querySelectorAll('.mm-scroll');
                        strips.forEach((stripEl) => {
                            const i = parseInt(stripEl.getAttribute('data-sec-idx'), 10);
                            const urls = (sections[i] && sections[i].urls) ? sections[i].urls.slice() : [];
                            stripEl.querySelectorAll('.mm-thumb').forEach((el, idx) => {
                                el.addEventListener('click', () => {
                                    try {
                                        window.MapMe && typeof window.MapMe.openPhotoViewer === 'function' ? window.MapMe.openPhotoViewer(urls, idx) : openPhotoViewer(urls, idx);
                                    } catch (_) {
                                    }
                                }, {once: true});
                            });
                        });

                        // Popover for user link hover/click
                        ensureUIStyles();
                        let popTimer = null;
                        let activePopover = null;
                        const showPopover = async (anchor, username, avatar) => {
                            hidePopover();
                            const rect = anchor.getBoundingClientRect();
                            const pop = document.createElement('div');
                            pop.className = 'mm-popover';
                            pop.innerHTML = `
                                <div class=\"mm-pop-inner\">
                                  <div style=\"display:flex;align-items:center;gap:8px;margin-bottom:8px;\">
                                    <img src=\"${avatar || '/images/user-avatar.svg'}\" alt=\"${escapeHtml(username)}\" style=\"width:40px;height:40px;border-radius:50%;object-fit:cover;\">
                                    <div>
                                      <div style=\"font-weight:700;\" class=\"mm-pop-name\">${escapeHtml(username)}</div>
                                      <div style=\"color:#6b7280;font-size:12px;\" class=\"mm-pop-handle\">@${escapeHtml(username)}</div>
                                    </div>
                                  </div>
                                  <div class=\"mm-pop-body\" style=\"font-size:12px;color:#374151;\">Loading profile…</div>
                                  <div class=\"mm-pop-photos\" style=\"display:flex;gap:6px;overflow-x:auto;margin-top:8px;\"></div>
                                  <div style=\"display:flex;gap:8px;margin-top:10px;\">
                                    <button class=\"mm-btn\" data-go=\"/user/${encodeURIComponent(username)}\">View profile →</button>
                                    <button class=\"mm-btn\" data-msg=\"/messages/new?to=${encodeURIComponent(username)}\">Message</button>
                                  </div>
                                </div>`;
                            document.body.appendChild(pop);
                            const top = window.scrollY + rect.top + rect.height + 6;
                            const left = window.scrollX + rect.left;
                            pop.style.top = `${top}px`;
                            pop.style.left = `${left}px`;
                            pop.addEventListener('mouseenter', () => {
                                if (popTimer) {
                                    clearTimeout(popTimer);
                                    popTimer = null;
                                }
                            });
                            pop.addEventListener('mouseleave', () => {
                                hidePopover(150);
                            });
                            const btn = pop.querySelector('button.mm-btn[data-go]');
                            if (btn) btn.addEventListener('click', (e) => {
                                e.preventDefault();
                                const dest = btn.getAttribute('data-go');
                                // Only allow safe relative URLs (defense-in-depth)
                                if (typeof dest === 'string' && /^\/user\//.test(dest)) {
                                    window.location.href = dest;
                                } else {
                                    // optionally log or display error, but do nothing
                                    console.warn('Unsafe navigation path blocked:', dest);
                                }
                            });
                            const msgBtn = pop.querySelector('button.mm-btn[data-msg]');
                            if (msgBtn) msgBtn.addEventListener('click', (e) => {
                                e.preventDefault();
                                window.location.href = msgBtn.getAttribute('data-msg');
                            });
                            activePopover = pop;
                            try {
                                const profile = await getUserProfile(username, avatar);
                                const body = pop.querySelector('.mm-pop-body');
                                const photosEl = pop.querySelector('.mm-pop-photos');
                                if (body) {
                                    const lines = [];
                                    if (profile.fullName) lines.push(`<div><strong>Name:</strong> ${escapeHtml(profile.fullName)}</div>`);
                                    if (profile.username) lines.push(`<div><strong>Handle:</strong> @${escapeHtml(profile.username)}</div>`);
                                    if (profile.bio) lines.push(`<div style=\"margin-top:4px;\">${escapeHtml(profile.bio)}</div>`);
                                    if (profile.location) lines.push(`<div><strong>Location:</strong> ${escapeHtml(profile.location)}</div>`);
                                    if (profile.website) lines.push(`<div><strong>Website:</strong> <a href=\"${profile.website}\" target=\"_blank\" rel=\"noopener\">${escapeHtml(profile.website)}</a></div>`);
                                    if (profile.joinedAt) lines.push(`<div><strong>Joined:</strong> ${escapeHtml(profile.joinedAt)}</div>`);
                                    const stats = [];
                                    if (profile.followers != null) stats.push(`${profile.followers} followers`);
                                    if (profile.following != null) stats.push(`${profile.following} following`);
                                    if (profile.photosCount != null) stats.push(`${profile.photosCount} photos`);
                                    if (stats.length) lines.push(`<div style=\"color:#6b7280\">${stats.join(' • ')}</div>`);
                                    if (Array.isArray(profile.interests) && profile.interests.length) {
                                        lines.push(`<div style=\"margin-top:6px;\"><strong>Interests:</strong> ${profile.interests.map(escapeHtml).join(', ')}</div>`);
                                    }
                                    body.innerHTML = lines.join('');
                                }
                                if (photosEl) {
                                    const urls = Array.isArray(profile.recentPhotos) ? profile.recentPhotos.slice(0, 10) : [];
                                    if (urls.length) {
                                        photosEl.innerHTML = urls.map(u => `<img src=\"${u}\" alt=\"\" style=\"width:44px;height:44px;border-radius:6px;object-fit:cover;border:1px solid #e9ecef;\"/>`).join('');
                                    } else {
                                        photosEl.style.display = 'none';
                                    }
                                }
                            } catch (e) {
                                // leave loading text
                            }
                        };
                        const hidePopover = (delay = 0) => {
                            if (popTimer) {
                                clearTimeout(popTimer);
                                popTimer = null;
                            }
                            const fn = () => {
                                if (activePopover && activePopover.parentNode) {
                                    activePopover.parentNode.removeChild(activePopover);
                                    activePopover = null;
                                }
                            };
                            if (delay > 0) popTimer = setTimeout(fn, delay); else fn();
                        };
                        container.querySelectorAll('.mm-user-link').forEach(a => {
                            const username = decodeURIComponent(a.getAttribute('data-username') || '');
                            const avatar = a.getAttribute('data-avatar');
                            a.addEventListener('mouseenter', () => showPopover(a, username, avatar));
                            a.addEventListener('mouseleave', () => hidePopover(150));
                            a.addEventListener('click', (e) => {
                                e.preventDefault();
                                showPopover(a, username, avatar);
                            });
                        });

                        // Add event handlers for edit buttons
                        container.querySelectorAll('.mm-edit-btn').forEach(btn => {
                            const dateMarkId = btn.getAttribute('data-datemark-id');
                            btn.addEventListener('click', (e) => {
                                e.preventDefault();
                                e.stopPropagation();
                                try {
                                    // Call the Blazor component to handle editing
                                    if (window.MapMe && window.MapMe.editDateMark) {
                                        window.MapMe.editDateMark(dateMarkId);
                                    } else {
                                        // Fallback: navigate to edit page
                                        window.location.href = `/map?edit=${encodeURIComponent(dateMarkId)}`;
                                    }
                                } catch (err) {
                                    console.error('Error editing Date Mark:', err);
                                }
                            });
                        });

                        // Handle Google Maps links in popup titles
                        const titleLinks = container.querySelectorAll('a[href*="google"], a[href*="maps"], a[target="_blank"]');
                        titleLinks.forEach(link => {
                            link.addEventListener('click', (e) => {
                                e.stopPropagation(); // Prevent marker click handler from interfering
                                // Let the default link behavior proceed (opening in new tab)
                            });
                        });
                    } catch (_) { /* ignore */
                    }
                }, 0);
            };

            mk.addListener('click', showGroupInfo);
            savedMarkers.push(mk);
        });
    } catch (e) {
        console.error('renderMarks error', e);
    }
}

// Basic HTML escape for displaying user-provided content in InfoWindow
function escapeHtml(str) {
    if (typeof str !== 'string') return str;
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

// Expose to global and export
window.MapMe = window.MapMe || {};
window.MapMe.renderMarks = renderMarks;
export {renderMarks};

// Lightweight photo viewer (lightbox) with zoom controls
let _mmViewerInjected = false;

function ensureViewerStyles() {
    if (_mmViewerInjected) return;
    _mmViewerInjected = true;
    const style = document.createElement('style');
    style.textContent = `
    .mm-viewer-overlay{position:fixed;inset:0;background:rgba(0,0,0,0.85);display:flex;align-items:center;justify-content:center;z-index:99999}
    .mm-viewer-content{position:relative;max-width:90vw;max-height:90vh}
    .mm-viewer-img{max-width:90vw;max-height:90vh;transform:scale(1);transition:transform .15s ease;cursor:grab;border-radius:8px;box-shadow:0 8px 24px rgba(0,0,0,.4)}
    .mm-viewer-controls{position:absolute;top:8px;right:8px;display:flex;gap:8px}
    .mm-btn{background:#ffffff; border:0; padding:8px 10px; border-radius:6px; cursor:pointer; font-weight:600}
    `;
    document.head.appendChild(style);
}

// Lightweight UI styles for popovers etc.
let _mmUIInjected = false;

function ensureUIStyles() {
    if (_mmUIInjected) return;
    _mmUIInjected = true;
    const style = document.createElement('style');
    style.textContent = `
    .mm-popover{position:absolute;background:#fff;border:1px solid #e5e7eb;border-radius:8px;box-shadow:0 8px 24px rgba(0,0,0,.12);padding:10px;z-index:100000}
    .mm-pop-inner{max-width:240px}
    .mm-popover .mm-btn{background:#0d6efd;color:#fff}
    .mm-popover .mm-btn:hover{filter:brightness(0.95)}
    `;
    document.head.appendChild(style);
}

function openPhotoViewer(urls, startIndex = 0) {
    try {
        ensureViewerStyles();
        const list = Array.isArray(urls) ? urls.filter(Boolean) : [];
        if (!list.length) return;
        let idx = Math.max(0, Math.min(startIndex, list.length - 1));
        let scale = 1;
        const overlay = document.createElement('div');
        overlay.className = 'mm-viewer-overlay';
        const content = document.createElement('div');
        content.className = 'mm-viewer-content';
        const img = document.createElement('img');
        img.className = 'mm-viewer-img';
        img.src = list[idx];
        const ctrls = document.createElement('div');
        ctrls.className = 'mm-viewer-controls';
        const btnIn = document.createElement('button');
        btnIn.className = 'mm-btn';
        btnIn.textContent = '+';
        const btnOut = document.createElement('button');
        btnOut.className = 'mm-btn';
        btnOut.textContent = '-';
        const btnNext = document.createElement('button');
        btnNext.className = 'mm-btn';
        btnNext.textContent = '›';
        const btnPrev = document.createElement('button');
        btnPrev.className = 'mm-btn';
        btnPrev.textContent = '‹';
        const btnClose = document.createElement('button');
        btnClose.className = 'mm-btn';
        btnClose.textContent = '×';
        ctrls.append(btnPrev, btnIn, btnOut, btnNext, btnClose);
        content.append(img, ctrls);
        overlay.appendChild(content);
        document.body.appendChild(overlay);

        const applyScale = () => {
            img.style.transform = `scale(${scale})`;
        };
        const close = () => {
            try {
                document.body.removeChild(overlay);
            } catch (_) {
            }
            window.removeEventListener('keydown', onKey);
        };
        const showIdx = (i) => {
            idx = (i + list.length) % list.length;
            img.src = list[idx];
            scale = 1;
            applyScale();
        };
        const onKey = (e) => {
            if (e.key === 'Escape') close();
            if (e.key === 'ArrowRight') showIdx(idx + 1);
            if (e.key === 'ArrowLeft') showIdx(idx - 1);
        };
        btnIn.onclick = () => {
            scale = Math.min(4, scale + 0.25);
            applyScale();
        };
        btnOut.onclick = () => {
            scale = Math.max(0.5, scale - 0.25);
            applyScale();
        };
        btnNext.onclick = () => showIdx(idx + 1);
        btnPrev.onclick = () => showIdx(idx - 1);
        btnClose.onclick = close;
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) close();
        });
        window.addEventListener('keydown', onKey);
        // Basic drag to pan
        let dragging = false, sx = 0, sy = 0, ox = 0, oy = 0;
        img.addEventListener('mousedown', (e) => {
            dragging = true;
            sx = e.clientX;
            sy = e.clientY;
            img.style.cursor = 'grabbing';
            ox = img.offsetLeft;
            oy = img.offsetTop;
            e.preventDefault();
        });
        window.addEventListener('mousemove', (e) => {
            if (!dragging) return;
            const dx = e.clientX - sx, dy = e.clientY - sy;
            img.style.transform = `translate(${dx}px, ${dy}px) scale(${scale})`;
        });
        window.addEventListener('mouseup', () => {
            if (!dragging) return;
            dragging = false;
            img.style.cursor = 'grab';
            img.style.transform = `scale(${scale})`;
        });
        // Wheel to zoom
        overlay.addEventListener('wheel', (e) => {
            e.preventDefault();
            const delta = e.deltaY > 0 ? -0.1 : 0.1;
            scale = Math.min(4, Math.max(0.5, scale + delta));
            applyScale();
        }, {passive: false});
    } catch (e) {
        console.error('openPhotoViewer error', e);
    }
}

window.MapMe = window.MapMe || {};
window.MapMe.openPhotoViewer = openPhotoViewer;

// Profile fetcher for popovers (tries app hook, then API, then mock)
const _mmProfileCache = new Map();

async function getUserProfile(username, avatarUrl) {
    try {
        const key = (username || '').toLowerCase();
        if (_mmProfileCache.has(key)) return _mmProfileCache.get(key);

        // 1) App-provided hook - use the Blazor UserProfileService
        if (window.MapMe && typeof window.MapMe.getUserProfile === 'function') {
            const prof = await window.MapMe.getUserProfile(username);
            if (prof) {
                _mmProfileCache.set(key, prof);
                return prof;
            }
        }

        // 2) REST API fallback (if implemented)
        try {
            const res = await fetch(`/api/users/${encodeURIComponent(username)}`);
            if (res.ok) {
                const prof = await res.json();
                _mmProfileCache.set(key, prof);
                return prof;
            }
        } catch (_) {
        }

        // 3) Default fallback with minimal real data
        const prof = {
            fullName: username || 'Unknown User',
            username: username || 'unknown',
            bio: 'MapMe user',
            location: 'Location not specified',
            website: null,
            joinedAt: 'Recently',
            followers: 0,
            following: 0,
            photosCount: 0,
            interests: [],
            avatar: avatarUrl || '/images/user-avatar.svg'
        };
        _mmProfileCache.set(key, prof);
        return prof;
    } catch (_) {
        return {
            fullName: username || 'Unknown User',
            username: username || 'unknown',
            avatar: avatarUrl || '/images/user-avatar.svg'
        };
    }
}

// ---- Debug helpers to mock multiple users and marks ----
function _mmCreateMockMarks(center) {
    const base = center || (map ? map.getCenter() : {lat: 37.7749, lng: -122.4194});
    const baseLat = typeof base.lat === 'function' ? base.lat() : base.lat;
    const baseLng = typeof base.lng === 'function' ? base.lng() : base.lng;
    const jitter = (meters) => {
        const dLat = (meters / 111320) * (Math.random() - 0.5);
        const dLng = (meters / (111320 * Math.cos(baseLat * Math.PI / 180))) * (Math.random() - 0.5);
        return {lat: baseLat + dLat, lng: baseLng + dLng};
    };
    const placePic = (seed, w = 300, h = 200) => `https://picsum.photos/seed/${encodeURIComponent(seed)}/${w}/${h}`;
    const userAvatar = (id) => `https://i.pravatar.cc/100?img=${id}`; // stable-ish avatars
    const userPhoto = (name, i) => `https://picsum.photos/seed/user-${encodeURIComponent(name)}-${i}/240/240`;

    // 1) Single user at P1
    const p1 = jitter(0);
    const m1 = {
        placeId: 'P1',
        lat: p1.lat, lng: p1.lng,
        title: 'Cafe Solo', address: '123 Market St',
        createdBy: 'alice',
        message: 'Best cappuccino in town ☕️',
        userPhotoUrl: userAvatar(5),
        userPhotos: Array.from({length: 6}, (_, i) => userPhoto('alice', i + 1)),
        placePhotoUrl: placePic('cafe-solo'),
        placePhotos: Array.from({length: 8}, (_, i) => placePic(`cafe-solo-${i + 1}`, 320, 200))
    };

    // 2) Two users at same P2
    const p2 = jitter(40);
    const m2a = {
        placeId: 'P2',
        lat: p2.lat,
        lng: p2.lng,
        title: 'Park View',
        address: '200 Green Rd',
        createdBy: 'bob',
        message: 'Great spot for jogging',
        userPhotoUrl: userAvatar(12),
        userPhotos: Array.from({length: 5}, (_, i) => userPhoto('bob', i + 1)),
        placePhotoUrl: placePic('park-view'),
        placePhotos: Array.from({length: 7}, (_, i) => placePic(`park-view-${i + 1}`, 320, 200))
    };
    const m2b = {
        placeId: 'P2',
        lat: p2.lat,
        lng: p2.lng,
        title: 'Park View',
        address: '200 Green Rd',
        createdBy: 'charlie',
        message: 'Picnic area is lovely!',
        userPhotoUrl: userAvatar(22),
        userPhotos: Array.from({length: 4}, (_, i) => userPhoto('charlie', i + 1)),
        placePhotoUrl: placePic('park-view'),
        placePhotos: Array.from({length: 7}, (_, i) => placePic(`park-view-${i + 1}`, 320, 200))
    };

    // 3) Three users at same P3
    const p3 = jitter(80);
    const m3a = {
        placeId: 'P3',
        lat: p3.lat,
        lng: p3.lng,
        title: 'Sky Bar',
        address: '45 Sunset Blvd',
        createdBy: 'dana',
        message: 'Sunset views are unreal 🌇',
        userPhotoUrl: userAvatar(30),
        userPhotos: Array.from({length: 6}, (_, i) => userPhoto('dana', i + 1)),
        placePhotoUrl: placePic('sky-bar'),
        placePhotos: Array.from({length: 6}, (_, i) => placePic(`sky-bar-${i + 1}`, 320, 200))
    };
    const m3b = {
        placeId: 'P3',
        lat: p3.lat,
        lng: p3.lng,
        title: 'Sky Bar',
        address: '45 Sunset Blvd',
        createdBy: 'ed',
        message: 'Cocktails are pricey but worth it',
        userPhotoUrl: userAvatar(31),
        userPhotos: Array.from({length: 5}, (_, i) => userPhoto('ed', i + 1)),
        placePhotoUrl: placePic('sky-bar'),
        placePhotos: Array.from({length: 6}, (_, i) => placePic(`sky-bar-${i + 1}`, 320, 200))
    };
    const m3c = {
        placeId: 'P3',
        lat: p3.lat,
        lng: p3.lng,
        title: 'Sky Bar',
        address: '45 Sunset Blvd',
        createdBy: 'frank',
        message: 'Live DJ on weekends 🎶',
        userPhotoUrl: userAvatar(32),
        userPhotos: Array.from({length: 7}, (_, i) => userPhoto('frank', i + 1)),
        placePhotoUrl: placePic('sky-bar'),
        placePhotos: Array.from({length: 6}, (_, i) => placePic(`sky-bar-${i + 1}`, 320, 200))
    };

    // 4) Five users at same P4 (tests +N counter -> +2)
    const p4 = jitter(120);
    const users4 = ['gina', 'henry', 'irene', 'jack', 'kate'];
    const messages4 = {
        gina: 'Street performers here are awesome!',
        henry: 'Plenty of benches to sit',
        irene: 'Fountain is beautiful at night',
        jack: 'Food trucks on Fridays',
        kate: 'Holiday market every December'
    };
    const marks4 = users4.map((name, idx) => ({
        placeId: 'P4',
        lat: p4.lat,
        lng: p4.lng,
        title: 'Central Plaza',
        address: '1 Main Sq',
        createdBy: name,
        message: messages4[name],
        userPhotoUrl: userAvatar(40 + idx),
        userPhotos: Array.from({length: 8}, (_, i) => userPhoto(name, i + 1)),
        placePhotoUrl: placePic('central-plaza'),
        placePhotos: Array.from({length: 10}, (_, i) => placePic(`central-plaza-${i + 1}`, 320, 200))
    }));

    // 5) No placeId; proximity cluster (~10m radius)
    const base5 = jitter(160);
    const prox = [0, 6, 9].map((r, i) => {
        const p = jitter(10); // within ~10m
        const uname = `user_${i + 1}`;
        return {
            lat: p.lat,
            lng: p.lng,
            title: 'Mystery Spot',
            address: 'Unknown Rd',
            createdBy: uname,
            message: `I found clue #${i + 1}`,
            userPhotoUrl: userAvatar(60 + i),
            userPhotos: Array.from({length: 5}, (_, k) => userPhoto(uname, k + 1)),
            placePhotoUrl: placePic('mystery-spot'),
            placePhotos: Array.from({length: 6}, (_, k) => placePic(`mystery-spot-${k + 1}`, 320, 200))
        };
    });

    return [m1, m2a, m2b, m3a, m3b, m3c, ...marks4, ...prox];
}

function debugRenderMockMarks() {
    try {
        if (!map) {
            console.warn('Map not initialized yet');
            return;
        }
        const mocks = _mmCreateMockMarks(map.getCenter());
        renderMarks(mocks);
    } catch (e) {
        console.error('debugRenderMockMarks error', e);
    }
}

window.MapMe = window.MapMe || {};
window.MapMe.debugRenderMockMarks = debugRenderMockMarks;
window.MapMe._mmCreateMockMarks = _mmCreateMockMarks; // exposed for testing
