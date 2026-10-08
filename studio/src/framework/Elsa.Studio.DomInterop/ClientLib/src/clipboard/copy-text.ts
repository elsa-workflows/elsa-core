export const copyText = async function (text) {
    await navigator.clipboard
        .writeText(text)
        .catch(function (error) {
            alert(error);
        });
}