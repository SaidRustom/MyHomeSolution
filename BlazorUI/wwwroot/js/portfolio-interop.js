let _portfolioScrollHandler = null;

window.portfolioScrollInit = (dotNetRef) => {
    _portfolioScrollHandler = () => {
        dotNetRef.invokeMethodAsync('OnScroll', window.scrollY > 50);
    };
    window.addEventListener('scroll', _portfolioScrollHandler, { passive: true });
};

window.portfolioScrollDestroy = () => {
    if (_portfolioScrollHandler) {
        window.removeEventListener('scroll', _portfolioScrollHandler);
        _portfolioScrollHandler = null;
    }
};
