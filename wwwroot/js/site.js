// site.js

// Navbar elementini seçiyoruz
const navbar = document.querySelector('.topbar');

// Mouse navbar üzerine gelince "active" class'ı eklenir
navbar.addEventListener('mouseenter', () => {
    navbar.classList.add('active');
});

// Mouse ayrılınca "active" class'ı kaldırılır
navbar.addEventListener('mouseleave', () => {
    navbar.classList.remove('active');
});

const logo = document.querySelector('.navbar-brand img');

navbar.addEventListener('mouseenter', () => {
    navbar.classList.add('active');
    logo.src = '/images/logo-blue.png'; // hover'da değişen logo
});

navbar.addEventListener('mouseleave', () => {
    navbar.classList.remove('active');
    logo.src = '/images/logo-blue.png'; // orijinal logo
});
