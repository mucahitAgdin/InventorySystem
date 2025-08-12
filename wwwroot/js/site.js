// site.js
const navbar = document.querySelector('.topbar');
const logoImg = document.querySelector('.navbar-brand img');

if (navbar && logoImg) {
    const whiteLogo = '/images/faurecia_inspiring_white.png';
    const blueLogo = '/images/faurecia_inspiring_blue.png';

    // Başlangıç: şeffaf navbar ise beyaz logo
    if (!navbar.classList.contains('active')) {
        logoImg.src = whiteLogo;
    }

    navbar.addEventListener('mouseenter', () => {
        navbar.classList.add('active');
        logoImg.src = blueLogo; // beyaz zemin -> mavi logo
    });

    navbar.addEventListener('mouseleave', () => {
        navbar.classList.remove('active');
        logoImg.src = whiteLogo; // koyu/şeffaf zemin -> beyaz logo
    });
}
