window.getCurrentLocation = async () => {
    return new Promise((resolve) => {
        console.log("Starting geolocation request...");
        
        if (!navigator.geolocation) {
            console.error("Geolocation not supported");
            resolve({
                success: false,
                latitude: 0,
                longitude: 0,
                error: "Geolocation is not supported by this browser."
            });
            return;
        }

        let watchId = null;
        let timeoutId = null;
        let resolved = false;

        // Set overall timeout
        timeoutId = setTimeout(() => {
            if (!resolved) {
                resolved = true;
                if (watchId !== null) {
                    navigator.geolocation.clearWatch(watchId);
                }
                console.error("Geolocation overall timeout");
                resolve({
                    success: false,
                    latitude: 0,
                    longitude: 0,
                    error: "Location request timed out. Your device may be taking too long to determine location."
                });
            }
        }, 25000); // 25 second overall timeout

        // Try getCurrentPosition first (faster if cached)
        navigator.geolocation.getCurrentPosition(
            (position) => {
                if (!resolved) {
                    resolved = true;
                    clearTimeout(timeoutId);
                    console.log("Location obtained:", position.coords.latitude, position.coords.longitude);
                    resolve({
                        success: true,
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        error: null
                    });
                }
            },
            (error) => {
                console.log("getCurrentPosition failed, trying watchPosition...", error.code);
                
                // If getCurrentPosition fails, try watchPosition (more reliable)
                const options = {
                    enableHighAccuracy: false,  // Start with false for speed
                    timeout: 20000,
                    maximumAge: 60000  // Accept cached location up to 1 minute
                };

                watchId = navigator.geolocation.watchPosition(
                    (position) => {
                        if (!resolved) {
                            resolved = true;
                            clearTimeout(timeoutId);
                            if (watchId !== null) {
                                navigator.geolocation.clearWatch(watchId);
                            }
                            console.log("Location obtained via watchPosition:", position.coords.latitude, position.coords.longitude);
                            resolve({
                                success: true,
                                latitude: position.coords.latitude,
                                longitude: position.coords.longitude,
                                error: null
                            });
                        }
                    },
                    (watchError) => {
                        if (!resolved) {
                            let errorMessage = "Unknown error occurred";
                            switch(watchError.code) {
                                case watchError.PERMISSION_DENIED:
                                    errorMessage = "Location permission denied. Please allow location access in your browser settings.";
                                    break;
                                case watchError.POSITION_UNAVAILABLE:
                                    errorMessage = "Location information is unavailable. Please check Windows Location Services.";
                                    break;
                                case watchError.TIMEOUT:
                                    errorMessage = "Location request timed out. Please check Windows Location Services or try searching manually.";
                                    break;
                            }
                            
                            resolved = true;
                            clearTimeout(timeoutId);
                            if (watchId !== null) {
                                navigator.geolocation.clearWatch(watchId);
                            }
                            console.error("watchPosition error:", errorMessage);
                            resolve({
                                success: false,
                                latitude: 0,
                                longitude: 0,
                                error: errorMessage
                            });
                        }
                    },
                    options
                );
            },
            {
                enableHighAccuracy: false,
                timeout: 10000,  // 10 second timeout for initial attempt
                maximumAge: 60000  // Accept cached location
            }
        );
    });
};

