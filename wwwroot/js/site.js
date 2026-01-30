window.downloadFile = (url, filename) => {
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
};
window.backToTop = (function () {
    let handler = null;
  
    function init(dotnetRef, threshold) {
      threshold = threshold ?? 300;
  
      handler = () => {
        const y = window.scrollY || document.documentElement.scrollTop || 0;
        dotnetRef.invokeMethodAsync("SetBackToTopVisible", y > threshold);
      };
  
      window.addEventListener("scroll", handler, { passive: true });
      handler();
    }
  
    function scrollTop() {
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  
    function dispose() {
      if (handler) {
        window.removeEventListener("scroll", handler);
        handler = null;
      }
    }
  
    return { init, scrollTop, dispose };
  })();
