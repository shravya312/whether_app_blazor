window.applyTheme = (theme) => {
    document.documentElement.setAttribute('data-theme', theme);
    document.body.className = theme === 'dark' ? 'dark-theme' : 'light-theme';
};

window.applyWeatherTheme = (weatherTheme) => {
    document.documentElement.setAttribute('data-weather-theme', weatherTheme);
    const body = document.body;
    body.classList.remove('sunny', 'cloudy', 'rainy', 'stormy', 'snowy', 'foggy', 'default');
    body.classList.add(weatherTheme);
};

