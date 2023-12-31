window.setImageFromStream = async (imageElement, imageStream) => {
    const arrayBuffer = await imageStream.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);

    imageElement.onload = () => {
        URL.revokeObjectURL(url);
    }

    imageElement.src = url;
}

window.setImageSrc = async (imageElement, src) => {
    imageElement.src = src;
}