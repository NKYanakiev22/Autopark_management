(() => {
  const menu = document.getElementById('mobile-menu');
  const sidebar = document.getElementById('sidebar');
  const backdrop = document.getElementById('mobile-backdrop');

  const closeMenu = () => {
    sidebar?.classList.remove('open');
    backdrop?.classList.remove('show');
  };

  menu?.addEventListener('click', () => {
    sidebar?.classList.toggle('open');
    backdrop?.classList.toggle('show');
  });
  backdrop?.addEventListener('click', closeMenu);
  sidebar?.querySelectorAll('.nav-link').forEach(link => link.addEventListener('click', closeMenu));
})();
