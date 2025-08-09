// Google Maps instance
let map;
let marker;
let clickListener;
let mapsApiLoaded = false;
// Keep track of rendered saved markers
let savedMarkers = [];
let sharedInfoWindow = null;
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

        // Small prompt before opening the full dialog
        const showDateProposalPrompt = ({ position, title, address, photos, onConfirm }) => {
            try {
                if (!sharedInfoWindow) {
                    sharedInfoWindow = new google.maps.InfoWindow();
                }
                const safe = (s) => typeof s === 'string' ? s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;') : '';
                const list = Array.isArray(photos) && photos.length > 0 ? photos : ['/images/place-photo.svg'];
                const imgs = list
                    .map(u => `<img class=\"mm-thumb\" src=\"${u}\" style=\"width:72px;height:72px;border-radius:6px;object-fit:cover;border:1px solid #e9ecef;cursor:pointer;\"/>`) 
                    .join('');
                const content = `
                  <div style="min-width:220px; max-width:320px;">
                    ${title ? `<div style=\"font-weight:600;margin-bottom:2px;\">${safe(title)}</div>` : ''}
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
                try { sharedInfoWindow.close(); } catch (_) {}
                sharedInfoWindow.setPosition(position);
                sharedInfoWindow.open({ map });
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
                                try { window.MapMe && typeof window.MapMe.openPhotoViewer === 'function' ? window.MapMe.openPhotoViewer(urls, idx) : openPhotoViewer(urls, idx); } catch (_) {}
                            }, { once: true });
                        });
                    } catch (_) {}
                    if (createBtn) {
                        createBtn.onclick = () => {
                            try { sharedInfoWindow.close(); } catch (_) {}
                            onConfirm && onConfirm();
                        };
                    }
                    if (cancelBtn) {
                        cancelBtn.onclick = () => { try { sharedInfoWindow.close(); } catch (_) {} };
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
                try { console.debug('Map click at', e.latLng && e.latLng.toString(), 'placeId:', e.placeId); } catch (_) {}
                try {
                    // If a dialog/info window is currently open, close it and do not open a new one immediately
                    if (sharedInfoWindow && typeof sharedInfoWindow.getMap === 'function' && sharedInfoWindow.getMap()) {
                        try { sharedInfoWindow.close(); } catch (_) {}
                        return;
                    }
                    // If user clicked a POI on the map, e.placeId will be present
                    if (e.placeId) {
                        // Prevent default Maps behavior
                        e.stop();
                        const service = new google.maps.places.PlacesService(map);
                        const fields = [
                            'place_id', 'name', 'geometry', 'types', 'url', 'photos', 'formatted_address'
                        ];
                        service.getDetails({ placeId: e.placeId, fields }, (place, status) => {
                            if (status === google.maps.places.PlacesServiceStatus.OK && place) {
                                try {
                                    const loc = place.geometry && place.geometry.location
                                        ? { lat: place.geometry.location.lat(), lng: place.geometry.location.lng() }
                                        : { lat: lat, lng: lng };
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
                                                try { return typeof p.getUrl === 'function' ? p.getUrl({ maxWidth: 800, maxHeight: 600 }) : null; } catch (_) { return null; }
                                            }).filter(Boolean).slice(0, 12);
                                            if (photoUrls.length > 0) {
                                                placePhotoUrl = photoUrls[0];
                                            }
                                        }
                                    } catch (_) { /* ignore */ }
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
                                        onConfirm: () => {
                                            // Move marker after confirm
                                            marker.setPosition(new google.maps.LatLng(loc.lat, loc.lng));
                                            if (dotNetHelper) {
                                                dotNetHelper.invokeMethodAsync('OnPlaceDetails', details);
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
                                        service2.getDetails({ placeId: pid, fields: ['photos'] }, (pl, st) => {
                                            let thumbs = [];
                                            try {
                                                if (st === google.maps.places.PlacesServiceStatus.OK && pl && pl.photos && pl.photos.length) {
                                                    thumbs = pl.photos.slice(0,3).map(p => {
                                                        try { return typeof p.getUrl === 'function' ? p.getUrl({ maxWidth: 400, maxHeight: 300 }) : null; } catch (_) { return null; }
                                                    }).filter(Boolean);
                                                }
                                            } catch (_) {}
                                            showDateProposalPrompt({ position: pos, title, address, photos: thumbs, onConfirm: () => {
                                                marker.setPosition(pos);
                                                if (dotNetHelper) { dotNetHelper.invokeMethodAsync('OnMapClick', pos.lat(), pos.lng()); }
                                            }});
                                        });
                                    } else {
                                        showDateProposalPrompt({ position: pos, title, address, photos: [], onConfirm: () => {
                                            marker.setPosition(pos);
                                            if (dotNetHelper) { dotNetHelper.invokeMethodAsync('OnMapClick', pos.lat(), pos.lng()); }
                                        }});
                                    }
                                });
                            }
                        });
                        return; // handled via details path
                    }
                } catch (err) {
                    console.debug('No placeId on click or error while handling POI:', err);
                }

                // Regular map click without a placeId
                const pos = e.latLng;
                reverseGeocode(pos.lat(), pos.lng()).then(min => {
                    const title = (min && min.name) ? min.name : 'Selected location';
                    const address = (min && min.address) ? min.address : '';
                    const pid = min && min.placeId ? min.placeId : null;
                    if (pid) {
                        const service3 = new google.maps.places.PlacesService(map);
                        service3.getDetails({ placeId: pid, fields: ['photos'] }, (pl, st) => {
                            let thumbs = [];
                            try {
                                if (st === google.maps.places.PlacesServiceStatus.OK && pl && pl.photos && pl.photos.length) {
                                    thumbs = pl.photos.map(p => {
                                        try { return typeof p.getUrl === 'function' ? p.getUrl({ maxWidth: 800, maxHeight: 600 }) : null; } catch (_) { return null; }
                                    }).filter(Boolean);
                                }
                            } catch (_) {}
                            showDateProposalPrompt({ position: pos, title, address, photos: thumbs, onConfirm: () => {
                                marker.setPosition(pos);
                                if (dotNetHelper) { dotNetHelper.invokeMethodAsync('OnMapClick', pos.lat(), pos.lng()); }
                            }});
                        });
                    } else {
                        showDateProposalPrompt({ position: pos, title, address, photos: [], onConfirm: () => {
                            marker.setPosition(pos);
                            if (dotNetHelper) { dotNetHelper.invokeMethodAsync('OnMapClick', pos.lat(), pos.lng()); }
                        }});
                    }
                });
            });

            // Add marker drag end event
            marker.addListener('dragend', (e) => {
                const pos = marker.getPosition();
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnMarkerDragEnd', pos.lat(), pos.lng());
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
            try { sharedInfoWindow.close(); } catch (_) {}
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
export { setCenter };

// Removed legacy loader that embedded an API key. Keys must be provided at runtime.

// Reverse geocode a coordinate to a friendly name/address
async function reverseGeocode(lat, lng) {
    if (!mapsApiLoaded) {
        console.error('Google Maps API not loaded');
        return null;
    }
    try {
        const geocoder = new google.maps.Geocoder();
        const latlng = { lat: Number(lat), lng: Number(lng) };
        return await new Promise((resolve) => {
            geocoder.geocode({ location: latlng }, (results, status) => {
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
        try { localStorage.setItem(key, value); } catch (_) {}
    },
    load: (key) => {
        try { return localStorage.getItem(key); } catch (_) { return null; }
    }
};

// Make helpers available on window and as module exports
window.MapMe.reverseGeocode = reverseGeocode;
export { reverseGeocode };

// Render a collection of saved marks on the map using user's first photo as the marker icon
function renderMarks(marks) {
    if (!mapsApiLoaded || !map) {
        // Map not ready yet; ignore safely
        return;
    }
    try {
        // Remove existing saved markers
        if (savedMarkers && savedMarkers.length) {
            savedMarkers.forEach(m => m.setMap(null));
        }
        savedMarkers = [];

        if (!Array.isArray(marks) || marks.length === 0) {
            return;
        }

        // Reuse a single info window
        if (!sharedInfoWindow) {
            sharedInfoWindow = new google.maps.InfoWindow();
        }

        marks.forEach(m => {
            const pos = { lat: Number(m.lat), lng: Number(m.lng) };
            // Prepare photos early to pick first user image for marker icon
            const userPhotosArr = [];
            if (Array.isArray(m.userPhotoUrls) && m.userPhotoUrls.length) userPhotosArr.push(...m.userPhotoUrls);
            if (m.userPhotoUrl) userPhotosArr.push(m.userPhotoUrl);
            let uniqueUser = [...new Set(userPhotosArr.filter(Boolean))];
            // Prefer non-avatar if available
            const avatarPath = '/images/user-avatar.svg';
            if (uniqueUser.length > 1) {
                uniqueUser = uniqueUser.filter(u => u !== avatarPath);
            }
            if (!uniqueUser.length) uniqueUser.push(avatarPath);

            const placePhotosArr = [];
            if (Array.isArray(m.placePhotoUrls) && m.placePhotoUrls.length) placePhotosArr.push(...m.placePhotoUrls);
            if (m.placePhotoUrl) placePhotosArr.push(m.placePhotoUrl);
            const uniquePlace = [...new Set(placePhotosArr.filter(Boolean))];
            if (!uniquePlace.length) uniquePlace.push('/images/place-photo.svg');

            const firstUserPhoto = uniqueUser[0] || avatarPath;
            const icon = {
                url: firstUserPhoto,
                scaledSize: new google.maps.Size(52, 52),
                anchor: new google.maps.Point(26, 26)
            };

            const mk = new google.maps.Marker({
                position: pos,
                map: map,
                icon: icon,
                title: m.title || 'Saved mark',
                clickable: true
            });

            mk.addListener('click', () => {
                const title = m.title ? `<div style="font-weight:600;">${escapeHtml(m.title)}</div>` : '';
                const addr = m.address ? `<div style=\"color:#6c757d; font-size:12px;\">${escapeHtml(m.address)}</div>` : '';
                const note = m.note ? `<div style=\"margin-top:6px;\">${escapeHtml(m.note)}</div>` : '';
                const byName = m.createdBy ? escapeHtml(m.createdBy) : '';
                const by = byName
                    ? `<div style=\"color:#6c757d; font-size:12px; margin-top:4px;\">By: <a href=\"/user/${encodeURIComponent(byName)}\" style=\"text-decoration:none;\">${byName}</a></div>`
                    : '';
                // uniqueUser and uniquePlace already prepared above

                const thumbHtml = (url) => `<img class=\"mm-thumb\" src=\"${url}\" alt=\"Photo\" style=\"width:72px;height:72px;border-radius:8px;object-fit:cover;border:1px solid #e9ecef;cursor:pointer;\"/>`;
                const userStrip = `<div class=\"mm-scroll\" style=\"display:flex; gap:8px; overflow-x:auto; padding-bottom:4px; margin:8px 0;\">${uniqueUser.map(thumbHtml).join('')}</div>`;
                const placeStrip = `<div class=\"mm-scroll\" style=\"display:flex; gap:8px; overflow-x:auto; padding-bottom:4px; margin:8px 0;\">${uniquePlace.map(thumbHtml).join('')}</div>`;
                const content = `<div style=\"max-width:320px;\">${title}${addr}${userStrip}${placeStrip}${note}${by}</div>`;
                sharedInfoWindow.setContent(content);
                sharedInfoWindow.open({ map, anchor: mk });
                // After open, attach click handlers for lightbox
                setTimeout(() => {
                    try {
                        const container = document.querySelector('.gm-style-iw, .gm-style-iw-c')?.parentElement || document.body;
                        const strips = container.querySelectorAll('.mm-scroll');
                        if (strips[0]) {
                            const userUrls = uniqueUser.slice();
                            strips[0].querySelectorAll('.mm-thumb').forEach((el, idx) => {
                                el.addEventListener('click', () => {
                                    try { window.MapMe && typeof window.MapMe.openPhotoViewer === 'function' ? window.MapMe.openPhotoViewer(userUrls, idx) : openPhotoViewer(userUrls, idx); } catch (_) {}
                                }, { once: true });
                            });
                        }
                        if (strips[1]) {
                            const placeUrls = uniquePlace.slice();
                            strips[1].querySelectorAll('.mm-thumb').forEach((el, idx) => {
                                el.addEventListener('click', () => {
                                    try { window.MapMe && typeof window.MapMe.openPhotoViewer === 'function' ? window.MapMe.openPhotoViewer(placeUrls, idx) : openPhotoViewer(placeUrls, idx); } catch (_) {}
                                }, { once: true });
                            });
                        }
                    } catch (_) { /* ignore */ }
                }, 0);
            });

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
export { renderMarks };

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
        const btnIn = document.createElement('button'); btnIn.className = 'mm-btn'; btnIn.textContent = '+';
        const btnOut = document.createElement('button'); btnOut.className = 'mm-btn'; btnOut.textContent = '-';
        const btnNext = document.createElement('button'); btnNext.className = 'mm-btn'; btnNext.textContent = '›';
        const btnPrev = document.createElement('button'); btnPrev.className = 'mm-btn'; btnPrev.textContent = '‹';
        const btnClose = document.createElement('button'); btnClose.className = 'mm-btn'; btnClose.textContent = '×';
        ctrls.append(btnPrev, btnIn, btnOut, btnNext, btnClose);
        content.append(img, ctrls);
        overlay.appendChild(content);
        document.body.appendChild(overlay);

        const applyScale = () => { img.style.transform = `scale(${scale})`; };
        const close = () => { try { document.body.removeChild(overlay); } catch (_) {} window.removeEventListener('keydown', onKey); };
        const showIdx = (i) => { idx = (i + list.length) % list.length; img.src = list[idx]; scale = 1; applyScale(); };
        const onKey = (e) => { if (e.key === 'Escape') close(); if (e.key === 'ArrowRight') showIdx(idx+1); if (e.key === 'ArrowLeft') showIdx(idx-1); };
        btnIn.onclick = () => { scale = Math.min(4, scale + 0.25); applyScale(); };
        btnOut.onclick = () => { scale = Math.max(0.5, scale - 0.25); applyScale(); };
        btnNext.onclick = () => showIdx(idx + 1);
        btnPrev.onclick = () => showIdx(idx - 1);
        btnClose.onclick = close;
        overlay.addEventListener('click', (e) => { if (e.target === overlay) close(); });
        window.addEventListener('keydown', onKey);
        // Basic drag to pan
        let dragging = false, sx=0, sy=0, ox=0, oy=0;
        img.addEventListener('mousedown', (e)=>{ dragging=true; sx=e.clientX; sy=e.clientY; img.style.cursor='grabbing'; ox = img.offsetLeft; oy = img.offsetTop; e.preventDefault(); });
        window.addEventListener('mousemove', (e)=>{ if(!dragging) return; const dx=e.clientX-sx, dy=e.clientY-sy; img.style.transform = `translate(${dx}px, ${dy}px) scale(${scale})`; });
        window.addEventListener('mouseup', ()=>{ if(!dragging) return; dragging=false; img.style.cursor='grab'; img.style.transform = `scale(${scale})`; });
        // Wheel to zoom
        overlay.addEventListener('wheel', (e)=>{ e.preventDefault(); const delta = e.deltaY > 0 ? -0.1 : 0.1; scale = Math.min(4, Math.max(0.5, scale + delta)); applyScale(); }, { passive: false });
    } catch (e) { console.error('openPhotoViewer error', e); }
}

window.MapMe = window.MapMe || {};
window.MapMe.openPhotoViewer = openPhotoViewer;
