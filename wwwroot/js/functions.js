window.setTheme = (theme) => {
    document.getElementsByTagName('body')[0].style.display = 'none';
    let synclink = document.getElementById('theme');
    synclink.href = '_content/Syncfusion.Blazor.Themes/' + theme + '.css';
    setTimeout(function () {
        document.getElementsByTagName('body')[0].style.display = 'block';
    }, 200);
}

